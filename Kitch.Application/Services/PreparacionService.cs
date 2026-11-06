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

    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IListaCompraService _listaCompraService;
    private readonly ISustitucionService _sustitucionService;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public PreparacionService(
        IRepository<Receta> recetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Usuario> usuarioRepository,
        IListaCompraService listaCompraService,
        ISustitucionService sustitucionService,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _recetaRepository = recetaRepository;
        _stockRepository = stockRepository;
        _usuarioRepository = usuarioRepository;
        _listaCompraService = listaCompraService;
        _sustitucionService = sustitucionService;
    }

    public async Task<PrevisualizarPorcionesResponseDto> PrevisualizarRecalculoPorcionesAsync(PrevisualizarPorcionesRequestDto request)
    {
        ValidarPorciones(request.NuevasPorciones);
        var receta = await GetRecetaConIngredientesAsync(request.RecetaId, request.UsuarioId);

        var factor = (decimal)request.NuevasPorciones / PorcionesBase(receta);
        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(request.UsuarioId);

        var ingredientes = receta.IngredientesReceta
            .Select(ingrediente =>
            {
                var ajustada = Math.Round(ingrediente.Cantidad * factor, 2);
                stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var stock);
                var (compatible, enAlacena) = DisponibleEnUnidadReceta(stock, ingrediente.UnidadMedida);
                var faltante = compatible
                    ? Math.Max(0, Math.Round(ajustada - enAlacena, 2))
                    : ajustada;

                return new IngredienteAjustadoDto
                {
                    IngredienteId = ingrediente.IngredienteId,
                    Nombre = ingrediente.Ingrediente?.Nombre ?? $"#{ingrediente.IngredienteId}",
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
        int porciones) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await BloquearStockAsync(usuarioId);
            return await AgregarFaltantesEnTransaccionAsync(usuarioId, recetaId, porciones);
        });

    private async Task<IEnumerable<ItemListaCompraResponseDto>> AgregarFaltantesEnTransaccionAsync(
        int usuarioId,
        int recetaId,
        int porciones)
    {
        var preview = await PrevisualizarRecalculoPorcionesAsync(new PrevisualizarPorcionesRequestDto
        {
            UsuarioId = usuarioId,
            RecetaId = recetaId,
            NuevasPorciones = porciones
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

    public async Task<PrevisualizarDescuentoStockResponseDto> PrevisualizarDescuentoStockAsync(PrevisualizarDescuentoStockRequestDto request)
    {
        ValidarPorciones(request.PorcionesCocinadas);
        var receta = await GetRecetaConIngredientesAsync(request.RecetaId, request.UsuarioId);

        var factor = (decimal)request.PorcionesCocinadas / PorcionesBase(receta);
        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(request.UsuarioId);

        return new PrevisualizarDescuentoStockResponseDto
        {
            Receta = receta.Titulo,
            PorcionesCocinadas = request.PorcionesCocinadas,
            Ingredientes = receta.IngredientesReceta
                .Select(ingrediente =>
                {
                    stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var stock);
                    var (compatible, disponible) = DisponibleEnUnidadReceta(stock, ingrediente.UnidadMedida);
                    var necesaria = Math.Round(ingrediente.Cantidad * factor, 2);

                    return new IngredienteDescuentoDto
                    {
                        IngredienteId = ingrediente.IngredienteId,
                        Nombre = ingrediente.Ingrediente?.Nombre ?? $"#{ingrediente.IngredienteId}",
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
        IReadOnlyList<CantidadUtilizadaDto>? cantidadesUtilizadas = null) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await BloquearStockAsync(usuarioId);
            return await DescontarIngredientesEnTransaccionAsync(usuarioId, recetaId, porciones, cantidadesUtilizadas);
        });

    private async Task<DescuentoStockResultadoDto> DescontarIngredientesEnTransaccionAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<CantidadUtilizadaDto>? cantidadesUtilizadas)
    {
        ValidarPorciones(porciones);

        var receta = await GetRecetaConIngredientesAsync(recetaId, usuarioId);
        var factor = (decimal)porciones / PorcionesBase(receta);
        var personalizado = (cantidadesUtilizadas ?? [])
            .Where(item => item.IngredienteId > 0)
            .GroupBy(item => item.IngredienteId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Last().Cantidad);

        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(usuarioId);
        var descuentos = new List<(StockUsuario Stock, decimal CantidadStock, decimal CantidadReceta, string Nombre, string Unidad)>();
        var resultado = new DescuentoStockResultadoDto { Receta = receta.Titulo };

        foreach (var ingrediente in receta.IngredientesReceta)
        {
            var cantidadADescontar = personalizado.TryGetValue(ingrediente.IngredienteId, out var usada)
                ? usada
                : Math.Round(ingrediente.Cantidad * factor, 2);

            if (cantidadADescontar <= 0)
            {
                continue;
            }

            var nombre = ingrediente.Ingrediente?.Nombre ?? $"#{ingrediente.IngredienteId}";

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
            await _stockRepository.UpdateAsync(stock);
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
        var receta = await GetRecetaConIngredientesAsync(request.RecetaId, usuarioId);
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

        var cantidadNueva = Math.Round(original.Cantidad * (elegido.FactorEquivalencia <= 0 ? 1m : elegido.FactorEquivalencia), 2);
        var ingredientes = receta.IngredientesReceta
            .Select(item => item.IngredienteId == original.IngredienteId
                ? new IngredienteRecetaResponseDto
                {
                    Id = item.Id,
                    IngredienteId = elegido.IngredienteId,
                    Nombre = elegido.Nombre,
                    Cantidad = cantidadNueva,
                    UnidadMedida = item.UnidadMedida
                }
                : new IngredienteRecetaResponseDto
                {
                    Id = item.Id,
                    IngredienteId = item.IngredienteId,
                    Nombre = item.Ingrediente?.Nombre ?? $"#{item.IngredienteId}",
                    Cantidad = item.Cantidad,
                    UnidadMedida = item.UnidadMedida
                })
            .ToList();

        return new AplicarSustitutoResponseDto
        {
            RecetaId = receta.Id,
            Titulo = receta.Titulo,
            Mensaje = $"Reemplazamos {original.Ingrediente?.Nombre ?? "el ingrediente"} por {elegido.Nombre}.",
            Ingredientes = ingredientes
        };
    }

    public Task<DescuentoStockResultadoDto> DescontarIngredientesParcialAsync(int usuarioId, int recetaId, int porciones) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await BloquearStockAsync(usuarioId);
            return await DescontarParcialEnTransaccionAsync(usuarioId, recetaId, porciones);
        });

    private Task BloquearStockAsync(int usuarioId) =>
        _unidadDeTrabajo.BloquearRecursoAsync($"kitch:stock:usuario:{usuarioId}");

    private async Task<DescuentoStockResultadoDto> DescontarParcialEnTransaccionAsync(int usuarioId, int recetaId, int porciones)
    {
        var receta = await GetRecetaConIngredientesAsync(recetaId, usuarioId);
        var porcionesEfectivas = porciones > 0 ? Math.Min(porciones, MaxPorciones) : PorcionesBase(receta);
        var factor = (decimal)porcionesEfectivas / PorcionesBase(receta);

        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(usuarioId);
        var resultado = new DescuentoStockResultadoDto { Receta = receta.Titulo };

        foreach (var ingrediente in receta.IngredientesReceta)
        {
            var cantidadNecesaria = Math.Round(ingrediente.Cantidad * factor, 2);
            if (cantidadNecesaria <= 0)
            {
                continue;
            }

            var nombre = ingrediente.Ingrediente?.Nombre ?? $"#{ingrediente.IngredienteId}";
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
                await _stockRepository.UpdateAsync(stock);

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

    private async Task<Receta> GetRecetaConIngredientesAsync(int recetaId, int usuarioId)
    {
        var recetas = await _recetaRepository.FindWithIncludePathsAsync(
            receta => receta.Id == recetaId,
            $"{nameof(Receta.IngredientesReceta)}.{nameof(IngredienteReceta.Ingrediente)}");

        var receta = recetas.FirstOrDefault()
            ?? throw new KeyNotFoundException(AccesoReceta.MensajeNoEncontrada);

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (!AccesoReceta.Permitido(receta, usuarioId, usuario?.Rol))
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
