using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Application.Common;
using Kitch.Application.DTOs.ListaCompra;
using Kitch.Application.DTOs.Preparacion;
using Kitch.Application.DTOs.Recetas;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class PreparacionService : IPreparacionService
{
    private const int MaxPorciones = 100;
    private const int MaxTitulo = 200;
    private const int MaxDescripcion = 1000;
    private const int MaxPaso = 1000;

    private const string InstruccionAdaptarReceta =
        "Sos un chef argentino experto. Te paso una receta original (título, descripción y pasos de preparación) " +
        "y los reemplazos de ingredientes que eligió el usuario. Tenés que reescribir el título, la descripción y " +
        "TODOS los pasos en español rioplatense natural para que tengan total coherencia gramatical (género, número, " +
        "artículos: por ejemplo 'el arroz', nunca 'las arroz') y coherencia culinaria real (por ejemplo, si reemplaza " +
        "papa por arroz, adaptá la técnica de cocción en lugar de decir pelar el arroz). " +
        "Si el ingrediente reemplazado figura en el título (ej: 'Tortilla de Papas' con papa -> arroz), actualizá el " +
        "título de forma natural (ej: 'Tortilla de Arroz'). Mantené la misma cantidad exacta de pasos y su numeración " +
        "1..N. Respondé ÚNICAMENTE con JSON válido, sin markdown, con esta forma: " +
        "{\"titulo\":string,\"descripcion\":string,\"preparaciones\":[{\"numeroPaso\":number,\"descripcionPaso\":string}]}.";

    private static readonly JsonSerializerOptions JsonOptions = JsonIa.Opciones;

    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IListaCompraService _listaCompraService;
    private readonly ISustitucionService _sustitucionService;
    private readonly IAsistenteIaClient _asistenteIa;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public PreparacionService(
        IRepository<Receta> recetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Usuario> usuarioRepository,
        IListaCompraService listaCompraService,
        ISustitucionService sustitucionService,
        IAsistenteIaClient asistenteIa,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _recetaRepository = recetaRepository;
        _stockRepository = stockRepository;
        _usuarioRepository = usuarioRepository;
        _listaCompraService = listaCompraService;
        _sustitucionService = sustitucionService;
        _asistenteIa = asistenteIa;
    }

    public async Task<PrevisualizarPorcionesResponseDto> PrevisualizarRecalculoPorcionesAsync(PrevisualizarPorcionesRequestDto request)
    {
        ValidarPorciones(request.NuevasPorciones);
        var receta = await GetRecetaConIngredientesAsync(request.RecetaId, request.UsuarioId);

        var factor = (decimal)request.NuevasPorciones / PorcionesBase(receta);
        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(request.UsuarioId);
        var ingredientesEfectivos = await ResolverIngredientesEfectivosAsync(request.UsuarioId, receta, request.Sustituciones);

        var ingredientes = ingredientesEfectivos
            .Select(ingrediente =>
            {
                var ajustada = Math.Round(ingrediente.Cantidad * factor, 2);
                if (IngredientesBasicos.EsAgua(ingrediente.Nombre))
                {
                    return new IngredienteAjustadoDto
                    {
                        IngredienteId = ingrediente.IngredienteId,
                        Nombre = ingrediente.Nombre,
                        CantidadOriginal = ingrediente.Cantidad,
                        CantidadAjustada = ajustada,
                        UnidadMedida = ingrediente.UnidadMedida,
                        CantidadEnAlacena = ajustada,
                        UnidadEnAlacena = ingrediente.UnidadMedida,
                        UnidadCompatible = true,
                        CantidadFaltante = 0m,
                        AlcanzaStock = true
                    };
                }

                stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var stock);
                var (compatible, enAlacena) = DisponibleEnUnidadReceta(stock, ingrediente.UnidadMedida);
                var faltante = compatible
                    ? Math.Max(0, Math.Round(ajustada - enAlacena, 2))
                    : ajustada;

                return new IngredienteAjustadoDto
                {
                    IngredienteId = ingrediente.IngredienteId,
                    Nombre = ingrediente.Nombre,
                    CantidadOriginal = ingrediente.Cantidad,
                    CantidadAjustada = ajustada,
                    UnidadMedida = ingrediente.UnidadMedida,
                    CantidadEnAlacena = compatible ? enAlacena : stock?.Cantidad ?? 0m,
                    UnidadEnAlacena = stock?.UnidadMedida,
                    UnidadCompatible = compatible,
                    CantidadFaltante = faltante,
                    AlcanzaStock = faltante <= 0
                };
            })
            .ToList();

        var faltantes = ingredientes.Where(item => item.CantidadFaltante > 0).ToList();

        return new PrevisualizarPorcionesResponseDto
        {
            Receta = receta.Titulo,
            PorcionesOriginales = receta.Porciones,
            NuevasPorciones = request.NuevasPorciones,
            AlcanzaStock = faltantes.Count == 0,
            HayFaltantes = faltantes.Count > 0,
            Ingredientes = ingredientes,
            IngredientesFaltantes = faltantes
        };
    }

    public Task<IEnumerable<ItemListaCompraResponseDto>> AgregarFaltantesPorcionesAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones = null) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await BloquearStockAsync(usuarioId);
            return await AgregarFaltantesEnTransaccionAsync(usuarioId, recetaId, porciones, sustituciones);
        });

    private async Task<IEnumerable<ItemListaCompraResponseDto>> AgregarFaltantesEnTransaccionAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones)
    {
        var preview = await PrevisualizarRecalculoPorcionesAsync(new PrevisualizarPorcionesRequestDto
        {
            UsuarioId = usuarioId,
            RecetaId = recetaId,
            NuevasPorciones = porciones,
            Sustituciones = (sustituciones ?? []).ToList()
        });

        foreach (var faltante in preview.IngredientesFaltantes)
        {
            await _listaCompraService.CreateAsync(new ItemListaCompraCreateDto
            {
                UsuarioId = usuarioId,
                IngredienteId = faltante.IngredienteId,
                NombreArticulo = faltante.Nombre,
                CantidadFaltante = (float)faltante.CantidadFaltante,
                UnidadMedida = faltante.UnidadMedida
            });
        }

        return await _listaCompraService.GetByUsuarioIdAsync(usuarioId);
    }

    public Task<PrevisualizarPorcionesResponseDto> MarcarFaltanteDisponibleAsync(
        int usuarioId,
        MarcarFaltanteDisponibleRequestDto request) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await BloquearStockAsync(usuarioId);
            return await MarcarFaltanteDisponibleEnTransaccionAsync(usuarioId, request);
        });

    private async Task<PrevisualizarPorcionesResponseDto> MarcarFaltanteDisponibleEnTransaccionAsync(
        int usuarioId,
        MarcarFaltanteDisponibleRequestDto request)
    {
        var previsualizacion = new PrevisualizarPorcionesRequestDto
        {
            UsuarioId = usuarioId,
            RecetaId = request.RecetaId,
            NuevasPorciones = request.NuevasPorciones,
            Sustituciones = request.Sustituciones ?? []
        };

        var preview = await PrevisualizarRecalculoPorcionesAsync(previsualizacion);
        var faltante = preview.IngredientesFaltantes
            .FirstOrDefault(item => item.IngredienteId == request.IngredienteId);

        if (faltante is null)
        {
            return preview;
        }

        var stock = await _stockRepository.FirstOrDefaultAsync(item =>
            item.UsuarioId == usuarioId && item.IngredienteId == faltante.IngredienteId);

        if (stock is null)
        {
            await _stockRepository.AddAsync(new StockUsuario
            {
                UsuarioId = usuarioId,
                IngredienteId = faltante.IngredienteId,
                Cantidad = faltante.CantidadFaltante,
                UnidadMedida = faltante.UnidadMedida
            });
        }
        else if (stock.Cantidad <= 0)
        {
            stock.Cantidad = faltante.CantidadFaltante;
            stock.UnidadMedida = faltante.UnidadMedida;
            await _stockRepository.UpdateAsync(stock);
        }
        else if (UnidadesMedida.TryConvertir(faltante.CantidadFaltante, faltante.UnidadMedida, stock.UnidadMedida, out var convertida))
        {
            stock.Cantidad = Math.Round(stock.Cantidad + convertida, 2);
            await _stockRepository.UpdateAsync(stock);
        }
        else
        {
            throw new InvalidOperationException(
                $"En tu alacena tenés {faltante.Nombre} cargado en {stock.UnidadMedida} y la receta lo usa en {faltante.UnidadMedida}. " +
                "Actualizalo desde Mi alacena.");
        }

        return await PrevisualizarRecalculoPorcionesAsync(previsualizacion);
    }

    public async Task<PrevisualizarDescuentoStockResponseDto> PrevisualizarDescuentoStockAsync(PrevisualizarDescuentoStockRequestDto request)
    {
        ValidarPorciones(request.PorcionesCocinadas);
        var receta = await GetRecetaConIngredientesAsync(request.RecetaId, request.UsuarioId);

        var factor = (decimal)request.PorcionesCocinadas / PorcionesBase(receta);
        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(request.UsuarioId);
        var ingredientesEfectivos = await ResolverIngredientesEfectivosAsync(request.UsuarioId, receta, request.Sustituciones);

        return new PrevisualizarDescuentoStockResponseDto
        {
            Receta = receta.Titulo,
            PorcionesCocinadas = request.PorcionesCocinadas,
            Ingredientes = ingredientesEfectivos
                .Where(ingrediente => !IngredientesBasicos.EsAgua(ingrediente.Nombre))
                .Select(ingrediente =>
                {
                    stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var stock);
                    var (compatible, disponible) = DisponibleEnUnidadReceta(stock, ingrediente.UnidadMedida);
                    var necesaria = Math.Round(ingrediente.Cantidad * factor, 2);

                    return new IngredienteDescuentoDto
                    {
                        IngredienteId = ingrediente.IngredienteId,
                        Nombre = ingrediente.Nombre,
                        CantidadDisponible = compatible ? disponible : stock?.Cantidad ?? 0m,
                        CantidadNecesaria = necesaria,
                        CantidadPosterior = compatible ? Math.Max(0, Math.Round(disponible - necesaria, 2)) : stock?.Cantidad ?? 0m,
                        CantidadFaltante = compatible ? Math.Max(0, Math.Round(necesaria - disponible, 2)) : 0m,
                        UnidadMedida = ingrediente.UnidadMedida,
                        UnidadEnAlacena = stock?.UnidadMedida,
                        UnidadCompatible = compatible
                    };
                })
                .ToList()
        };
    }

    public Task<DescuentoStockResultadoDto> DescontarIngredientesAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<CantidadUtilizadaDto>? cantidadesUtilizadas = null,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones = null) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await BloquearStockAsync(usuarioId);
            return await DescontarIngredientesEnTransaccionAsync(
                usuarioId, recetaId, porciones, cantidadesUtilizadas, sustituciones);
        });

    private async Task<DescuentoStockResultadoDto> DescontarIngredientesEnTransaccionAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<CantidadUtilizadaDto>? cantidadesUtilizadas,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones)
    {
        ValidarPorciones(porciones);

        var receta = await GetRecetaConIngredientesAsync(recetaId, usuarioId);
        var ingredientesEfectivos = await ResolverIngredientesEfectivosAsync(usuarioId, receta, sustituciones);
        var factor = (decimal)porciones / PorcionesBase(receta);
        var personalizado = (cantidadesUtilizadas ?? [])
            .Where(item => item.IngredienteId > 0)
            .GroupBy(item => item.IngredienteId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Last().Cantidad);

        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(usuarioId);
        var descuentos = new List<(StockUsuario Stock, decimal CantidadStock, decimal CantidadReceta, string Nombre, string Unidad)>();
        var resultado = new DescuentoStockResultadoDto { Receta = receta.Titulo };

        foreach (var ingrediente in ingredientesEfectivos)
        {
            if (IngredientesBasicos.EsAgua(ingrediente.Nombre))
            {
                continue;
            }

            var cantidadADescontar = personalizado.TryGetValue(ingrediente.IngredienteId, out var usada)
                ? usada
                : Math.Round(ingrediente.Cantidad * factor, 2);

            if (cantidadADescontar <= 0)
            {
                continue;
            }

            var nombre = ingrediente.Nombre;

            if (!stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var stock))
            {
                throw new InvalidOperationException(
                    $"No hay stock cargado del ingrediente '{nombre}'.");
            }

            if (!UnidadesMedida.TryConvertir(cantidadADescontar, ingrediente.UnidadMedida, stock.UnidadMedida, out var enUnidadStock))
            {
                resultado.NoDescontados.Add(new IngredienteMovimientoStockDto
                {
                    Nombre = nombre,
                    Cantidad = cantidadADescontar,
                    UnidadMedida = ingrediente.UnidadMedida
                });
                continue;
            }

            if (stock.Cantidad < enUnidadStock)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente del ingrediente '{nombre}'. " +
                    $"Necesario: {enUnidadStock} {stock.UnidadMedida}, disponible: {stock.Cantidad} {stock.UnidadMedida}.");
            }

            descuentos.Add((stock, enUnidadStock, cantidadADescontar, nombre, ingrediente.UnidadMedida));
        }

        foreach (var (stock, cantidadStock, cantidadReceta, nombre, unidad) in descuentos)
        {
            stock.Cantidad = Math.Max(0, Math.Round(stock.Cantidad - cantidadStock, 2));
            await GuardarStockDescontadoAsync(stock);
            resultado.Descontados.Add(new IngredienteMovimientoStockDto
            {
                Nombre = nombre,
                Cantidad = cantidadReceta,
                UnidadMedida = unidad
            });
        }

        resultado.Mensaje = resultado.NoDescontados.Count == 0
            ? "Stock actualizado. ¡Buen provecho!"
            : "Stock actualizado. Algunos ingredientes están cargados en otra unidad y no se descontaron: ajustalos a mano en tu alacena.";

        return resultado;
    }

    public async Task<AplicarSustitutoResponseDto> AplicarSustitutoAsync(
        int usuarioId,
        AplicarSustitutoRequestDto request)
    {
        var receta = await GetRecetaConIngredientesAsync(request.RecetaId, usuarioId, incluirPreparaciones: true);
        var original = receta.IngredientesReceta
            .FirstOrDefault(item => item.IngredienteId == request.IngredienteOriginalId);

        if (original is null)
        {
            throw new InvalidOperationException("Ese ingrediente no forma parte de la receta.");
        }

        var sugeridos = (await _sustitucionService.BuscarSustitutosAsync(
            usuarioId, request.IngredienteOriginalId)).ToList();
        var elegido = sugeridos.FirstOrDefault(item => item.IngredienteId == request.IngredienteSustitutoId);

        if (elegido is null)
        {
            throw new InvalidOperationException(
                "No hay un reemplazo viable que mantenga la receta. Podés abortar o guardarla para otro momento.");
        }

        var activas = (request.SustitucionesActivas ?? [])
            .Where(item =>
                item.IngredienteOriginalId > 0 &&
                item.IngredienteSustitutoId > 0 &&
                item.IngredienteOriginalId != request.IngredienteOriginalId)
            .ToList();
        activas.Add(new SustitucionAplicadaDto
        {
            IngredienteOriginalId = request.IngredienteOriginalId,
            IngredienteSustitutoId = request.IngredienteSustitutoId
        });

        var efectivos = await ResolverIngredientesEfectivosAsync(usuarioId, receta, activas);
        var originalesPorId = receta.IngredientesReceta.ToDictionary(item => item.IngredienteId);
        var cambiosTexto = new List<string>();
        var ingredientes = new List<IngredienteRecetaResponseDto>(receta.IngredientesReceta.Count);

        var itemsOriginales = receta.IngredientesReceta.ToList();
        for (var i = 0; i < itemsOriginales.Count; i++)
        {
            var baseItem = itemsOriginales[i];
            var efectivo = efectivos[i];
            if (efectivo.IngredienteId != baseItem.IngredienteId)
            {
                var nombreBase = baseItem.Ingrediente?.Nombre ?? $"#{baseItem.IngredienteId}";
                cambiosTexto.Add($"- Reemplazar '{nombreBase}' por '{efectivo.Nombre}' ({efectivo.Cantidad} {efectivo.UnidadMedida})");
            }

            ingredientes.Add(new IngredienteRecetaResponseDto
            {
                Id = baseItem.Id,
                IngredienteId = efectivo.IngredienteId,
                Nombre = efectivo.Nombre,
                Cantidad = efectivo.Cantidad,
                UnidadMedida = efectivo.UnidadMedida
            });
        }

        var pasosBase = receta.Preparaciones
            .OrderBy(paso => paso.NumeroPaso)
            .Select((paso, index) => new PreparacionRecetaResponseDto
            {
                Id = paso.Id,
                NumeroPaso = index + 1,
                DescripcionPaso = paso.DescripcionPaso
            })
            .ToList();

        var adaptada = await AdaptarTextosRecetaConIaAsync(receta, pasosBase, cambiosTexto);
        var nombreOriginal = originalesPorId.TryGetValue(original.IngredienteId, out var orig)
            ? orig.Ingrediente?.Nombre ?? "el ingrediente"
            : "el ingrediente";

        return new AplicarSustitutoResponseDto
        {
            RecetaId = receta.Id,
            Titulo = adaptada.Titulo,
            Descripcion = adaptada.Descripcion,
            Mensaje = $"Reemplazamos {nombreOriginal} por {elegido.Nombre}.",
            Ingredientes = ingredientes,
            Preparaciones = adaptada.Preparaciones
        };
    }

    private async Task<(string Titulo, string Descripcion, List<PreparacionRecetaResponseDto> Preparaciones)>
        AdaptarTextosRecetaConIaAsync(
            Receta receta,
            List<PreparacionRecetaResponseDto> pasosBase,
            List<string> cambiosTexto)
    {
        if (cambiosTexto.Count == 0)
        {
            return (receta.Titulo, receta.Descripcion, pasosBase);
        }

        try
        {
            var pasosTexto = string.Join("\n", pasosBase.Select(p => $"{p.NumeroPaso}. {p.DescripcionPaso}"));
            var prompt =
                $"Receta original:\n" +
                $"Título: {receta.Titulo}\n" +
                $"Descripción: {receta.Descripcion}\n" +
                $"Pasos:\n{pasosTexto}\n\n" +
                $"Reemplazos elegidos:\n{string.Join("\n", cambiosTexto)}";

            var json = await _asistenteIa.GenerarRespuestaJsonAsync(prompt, InstruccionAdaptarReceta);
            var limpio = JsonIa.LimpiarBloqueCodigo(json);
            if (limpio.Length == 0)
            {
                return (receta.Titulo, receta.Descripcion, pasosBase);
            }

            var dto = JsonSerializer.Deserialize<RecetaAdaptadaIaDto>(limpio, JsonOptions);
            if (dto is null)
            {
                return (receta.Titulo, receta.Descripcion, pasosBase);
            }

            var titulo = RecortarTexto(dto.Titulo, MaxTitulo, receta.Titulo);
            var descripcion = RecortarTexto(dto.Descripcion, MaxDescripcion, receta.Descripcion);
            var pasosAdaptados = ResolverPasosAdaptados(dto.Preparaciones, pasosBase);

            return (titulo, descripcion, pasosAdaptados);
        }
        catch
        {
            return (receta.Titulo, receta.Descripcion, pasosBase);
        }
    }

    private static string RecortarTexto(string? valor, int maximo, string fallback)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return fallback;
        }

        var limpio = valor.Trim();
        return limpio.Length > maximo ? limpio[..maximo].TrimEnd() : limpio;
    }

    private static List<PreparacionRecetaResponseDto> ResolverPasosAdaptados(
        List<PasoAdaptadoIaDto>? generados,
        List<PreparacionRecetaResponseDto> pasosBase)
    {
        if (generados is null || generados.Count != pasosBase.Count)
        {
            return pasosBase;
        }

        var ordenados = generados
            .OrderBy(paso => paso.NumeroPaso)
            .ToList();

        var resultado = new List<PreparacionRecetaResponseDto>(pasosBase.Count);
        for (var i = 0; i < pasosBase.Count; i++)
        {
            var texto = RecortarTexto(ordenados[i].DescripcionPaso, MaxPaso, pasosBase[i].DescripcionPaso);
            resultado.Add(new PreparacionRecetaResponseDto
            {
                Id = pasosBase[i].Id,
                NumeroPaso = pasosBase[i].NumeroPaso,
                DescripcionPaso = texto
            });
        }

        return resultado;
    }

    private sealed class RecetaAdaptadaIaDto
    {
        [JsonPropertyName("titulo")]
        public string? Titulo { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("preparaciones")]
        public List<PasoAdaptadoIaDto>? Preparaciones { get; set; }
    }

    private sealed class PasoAdaptadoIaDto
    {
        [JsonPropertyName("numeroPaso")]
        public int NumeroPaso { get; set; }

        [JsonPropertyName("descripcionPaso")]
        public string? DescripcionPaso { get; set; }
    }

    public Task<DescuentoStockResultadoDto> DescontarIngredientesParcialAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones = null) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await BloquearStockAsync(usuarioId);
            return await DescontarParcialEnTransaccionAsync(usuarioId, recetaId, porciones, sustituciones);
        });

    private sealed record IngredienteEfectivo(int IngredienteId, string Nombre, decimal Cantidad, string UnidadMedida);

    private async Task<List<IngredienteEfectivo>> ResolverIngredientesEfectivosAsync(
        int usuarioId,
        Receta receta,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones)
    {
        var porOriginal = new Dictionary<int, IngredienteEfectivo>();

        foreach (var sustitucion in (sustituciones ?? [])
                     .Where(item => item.IngredienteOriginalId > 0 && item.IngredienteSustitutoId > 0)
                     .GroupBy(item => item.IngredienteOriginalId)
                     .Select(grupo => grupo.Last()))
        {
            var original = receta.IngredientesReceta
                .FirstOrDefault(item => item.IngredienteId == sustitucion.IngredienteOriginalId)
                ?? throw new InvalidOperationException("Uno de los ingredientes reemplazados no forma parte de la receta.");

            var elegido = (await _sustitucionService.BuscarSustitutosAsync(usuarioId, sustitucion.IngredienteOriginalId))
                .FirstOrDefault(item => item.IngredienteId == sustitucion.IngredienteSustitutoId)
                ?? throw new InvalidOperationException(
                    $"El reemplazo elegido para {original.Ingrediente?.Nombre ?? "un ingrediente"} ya no está disponible.");

            var factorEquivalencia = elegido.FactorEquivalencia <= 0 ? 1m : elegido.FactorEquivalencia;
            porOriginal[original.IngredienteId] = new IngredienteEfectivo(
                elegido.IngredienteId,
                elegido.Nombre,
                Math.Round(original.Cantidad * factorEquivalencia, 2),
                original.UnidadMedida);
        }

        return receta.IngredientesReceta
            .Select(item => porOriginal.TryGetValue(item.IngredienteId, out var sustituto)
                ? sustituto
                : new IngredienteEfectivo(
                    item.IngredienteId,
                    item.Ingrediente?.Nombre ?? $"#{item.IngredienteId}",
                    item.Cantidad,
                    item.UnidadMedida))
            .ToList();
    }

    private Task GuardarStockDescontadoAsync(StockUsuario stock) =>
        stock.Cantidad <= 0
            ? _stockRepository.DeleteAsync(stock)
            : _stockRepository.UpdateAsync(stock);

    private Task BloquearStockAsync(int usuarioId) =>
        _unidadDeTrabajo.BloquearRecursoAsync($"kitch:stock:usuario:{usuarioId}");

    private async Task<DescuentoStockResultadoDto> DescontarParcialEnTransaccionAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones)
    {
        var receta = await GetRecetaConIngredientesAsync(recetaId, usuarioId);
        var ingredientesEfectivos = await ResolverIngredientesEfectivosAsync(usuarioId, receta, sustituciones);
        var porcionesEfectivas = porciones > 0 ? Math.Min(porciones, MaxPorciones) : PorcionesBase(receta);
        var factor = (decimal)porcionesEfectivas / PorcionesBase(receta);

        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(usuarioId);
        var resultado = new DescuentoStockResultadoDto { Receta = receta.Titulo };

        foreach (var ingrediente in ingredientesEfectivos)
        {
            if (IngredientesBasicos.EsAgua(ingrediente.Nombre))
            {
                continue;
            }

            var cantidadNecesaria = Math.Round(ingrediente.Cantidad * factor, 2);
            if (cantidadNecesaria <= 0)
            {
                continue;
            }

            var nombre = ingrediente.Nombre;
            stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var stock);

            if (stock is not null && stock.Cantidad > 0 &&
                !UnidadesMedida.SonCompatibles(ingrediente.UnidadMedida, stock.UnidadMedida))
            {
                resultado.NoDescontados.Add(new IngredienteMovimientoStockDto
                {
                    Nombre = nombre,
                    Cantidad = cantidadNecesaria,
                    UnidadMedida = ingrediente.UnidadMedida
                });
                continue;
            }

            var (_, disponible) = DisponibleEnUnidadReceta(stock, ingrediente.UnidadMedida);
            var aDescontar = Math.Min(disponible, cantidadNecesaria);

            if (aDescontar > 0 && stock is not null &&
                UnidadesMedida.TryConvertir(aDescontar, ingrediente.UnidadMedida, stock.UnidadMedida, out var enUnidadStock))
            {
                stock.Cantidad = Math.Max(0, Math.Round(stock.Cantidad - enUnidadStock, 2));
                await GuardarStockDescontadoAsync(stock);

                resultado.Descontados.Add(new IngredienteMovimientoStockDto
                {
                    Nombre = nombre,
                    Cantidad = aDescontar,
                    UnidadMedida = ingrediente.UnidadMedida
                });
            }

            var faltante = Math.Round(cantidadNecesaria - aDescontar, 2);
            if (faltante > 0)
            {
                resultado.Faltantes.Add(new IngredienteMovimientoStockDto
                {
                    Nombre = nombre,
                    Cantidad = faltante,
                    UnidadMedida = ingrediente.UnidadMedida
                });
            }
        }

        resultado.Mensaje = resultado.Faltantes.Count == 0 && resultado.NoDescontados.Count == 0
            ? "Stock actualizado. ¡Buen provecho!"
            : resultado.NoDescontados.Count > 0
                ? "Descontamos lo que había en la alacena. Algunos ingredientes están en otra unidad y no se tocaron: ajustalos a mano."
                : "Descontamos lo que había en la alacena. Revisá los faltantes.";

        return resultado;
    }

    private static (bool Compatible, decimal Cantidad) DisponibleEnUnidadReceta(StockUsuario? stock, string unidadReceta)
    {
        if (stock is null || stock.Cantidad <= 0)
        {
            return (true, 0m);
        }

        return UnidadesMedida.TryConvertir(stock.Cantidad, stock.UnidadMedida, unidadReceta, out var convertida)
            ? (true, Math.Round(convertida, 2))
            : (false, 0m);
    }

    private async Task<Dictionary<int, StockUsuario>> ObtenerStockPorIngredienteAsync(int usuarioId)
    {
        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId);
        return stock
            .GroupBy(item => item.IngredienteId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());
    }

    private async Task<Receta> GetRecetaConIngredientesAsync(
        int recetaId,
        int usuarioId,
        bool incluirPreparaciones = false)
    {
        var rutas = incluirPreparaciones
            ? new[] { $"{nameof(Receta.IngredientesReceta)}.{nameof(IngredienteReceta.Ingrediente)}", nameof(Receta.Preparaciones) }
            : new[] { $"{nameof(Receta.IngredientesReceta)}.{nameof(IngredienteReceta.Ingrediente)}" };

        var recetas = await _recetaRepository.FindWithIncludePathsAsync(
            receta => receta.Id == recetaId,
            rutas);

        var receta = recetas.FirstOrDefault()
            ?? throw new KeyNotFoundException(AccesoReceta.MensajeNoEncontrada);

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (!AccesoReceta.PuedeVer(receta, usuarioId, usuario?.Rol))
        {
            throw new KeyNotFoundException(AccesoReceta.MensajeNoEncontrada);
        }

        if (!LimitesPlan.PuedeUsarDificultad(usuario?.Rol, receta.Dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }

        if (receta.IngredientesReceta.Count == 0)
        {
            throw new InvalidOperationException("La receta debe tener al menos un ingrediente.");
        }

        return receta;
    }

    private static int PorcionesBase(Receta receta) => receta.Porciones > 0 ? receta.Porciones : 1;

    private static void ValidarPorciones(int porciones)
    {
        if (porciones <= 0 || porciones > MaxPorciones)
        {
            throw new ArgumentException($"Las porciones deben estar entre 1 y {MaxPorciones}.");
        }
    }
}
