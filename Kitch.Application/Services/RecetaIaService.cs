using System.Text;
using System.Text.Json;
using Kitch.Application.Common;
using Kitch.Application.DTOs.Favoritos;
using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Kitch.Application.Services;

public class RecetaIaService : IRecetaIaService
{
    private const string InstruccionGeneracion =
        "Sos 'Kitch-AI', un chef experto de la plataforma Kitch. Tu tarea es crear UNA receta " +
        "usando preferentemente los ingredientes que el usuario tiene en su alacena. " +
        "Podés sumar ingredientes básicos comunes (sal, agua, aceite) si hacen falta. " +
        "Respondé ÚNICAMENTE con un objeto JSON válido, sin texto adicional ni markdown, con esta forma exacta: " +
        "{\"titulo\": string, \"descripcion\": string, \"tiempoPreparacionMinutos\": number, " +
        "\"porciones\": number, \"dificultad\": \"Facil\"|\"Medio\"|\"Dificil\", \"caloriasEstimadas\": number, " +
        "\"categoria\": \"pastas\"|\"carnes\"|\"pollo\"|\"ensaladas\"|\"sopas\"|\"pescados\"|\"pizzas\"|\"postres\"|\"tartas\"|\"guisos\"|\"general\", " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}], " +
        "\"pasos\": [string]}. " +
        "El campo categoria es OBLIGATORIO: elegí exactamente uno de esos valores, en minúsculas. " +
        "Si no encaja con claridad, usá \"general\". " +
        "El tiempo y las porciones deben ser mayores a cero, y tiene que haber al menos un ingrediente y un paso.";

    public static readonly string InstruccionPlanBasico =
        "PLAN BASICO: la dificultad SOLO puede ser \"Facil\" o \"Medio\" y el plato tiene que serlo de verdad: " +
        $"como máximo {ComplejidadReceta.PasosSugeridosBasico} pasos, {ComplejidadReceta.IngredientesSugeridosBasico} ingredientes " +
        $"y {ComplejidadReceta.MinutosSugeridosBasico} minutos, sin técnicas avanzadas (sous vide, templado, masa madre, laminado, flambeado, esferificación). " +
        "NUNCA etiquetes como \"Medio\" un plato que en realidad es difícil.";

    private const int MaxTitulo = 200;
    private const int MaxDescripcion = 1000;
    private const int MaxPaso = 1000;
    private const int MaxUnidad = 50;
    private const int MaxNombreIngrediente = 100;
    private const int MaxMinutos = 1440;
    private const int MaxPorciones = 100;
    private const int MaxCalorias = 20000;
    private const decimal MaxCantidad = 99999m;

    private static readonly JsonSerializerOptions JsonOptions = JsonIa.Opciones;

    private readonly IAsistenteIaClient _asistenteIa;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IFavoritoService _favoritoService;
    private readonly IIngredienteNormalizerService _normalizer;
    private readonly ICuotaIaService _cuotaIa;
    private readonly IMemoryCache _cache;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public RecetaIaService(
        IAsistenteIaClient asistenteIa,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRepository<Receta> recetaRepository,
        IRepository<Usuario> usuarioRepository,
        IFavoritoService favoritoService,
        IIngredienteNormalizerService normalizer,
        ICuotaIaService cuotaIa,
        IMemoryCache cache,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _asistenteIa = asistenteIa;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
        _recetaRepository = recetaRepository;
        _usuarioRepository = usuarioRepository;
        _favoritoService = favoritoService;
        _normalizer = normalizer;
        _cuotaIa = cuotaIa;
        _cache = cache;
    }

    public async Task<RecetaGeneradaDto> GenerarRecetaAsync(int usuarioId, string? preferencias)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (!RolUsuario.TieneAccesoPremium(usuario?.Rol) && PedidoRecetaPremium.EsPedido(preferencias))
        {
            throw new ForbiddenException(LimitesPlan.MensajePedidoDificilBasico);
        }

        await _cuotaIa.ConsumirAsync(usuarioId);

        try
        {
            return await GenerarConCuotaConsumidaAsync(usuarioId, usuario, preferencias);
        }
        catch
        {
            await _cuotaIa.DevolverAsync(usuarioId);
            throw;
        }
    }

    private async Task<RecetaGeneradaDto> GenerarConCuotaConsumidaAsync(int usuarioId, Usuario? usuario, string? preferencias)
    {
        var stock = await _stockRepository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId && item.Cantidad > 0,
            item => item.Ingrediente);

        var prompt = new StringBuilder();
        prompt.AppendLine("Ingredientes disponibles en la alacena del usuario:");

        if (stock.Count == 0)
        {
            prompt.AppendLine("- (la alacena está vacía: proponé una receta sencilla con ingredientes básicos)");
        }
        else
        {
            foreach (var item in stock)
            {
                var nombre = string.IsNullOrWhiteSpace(item.Ingrediente?.Nombre)
                    ? $"Ingrediente #{item.IngredienteId}"
                    : item.Ingrediente!.Nombre;
                prompt.AppendLine($"- {nombre}: {item.Cantidad} {item.UnidadMedida}");
            }
        }

        var restriccion = RestriccionDieteticaPrompt.ParaSystemPrompt(usuario?.PreferenciaDietetica);
        var systemInstruction = restriccion + " " + InstruccionGeneracion;

        var esBasico = !RolUsuario.TieneAccesoPremium(usuario?.Rol);
        if (esBasico)
        {
            systemInstruction += " " + InstruccionPlanBasico;
        }

        if (!string.IsNullOrWhiteSpace(preferencias))
        {
            prompt.AppendLine();
            prompt.AppendLine($"Preferencias o restricciones adicionales indicadas en este pedido: {preferencias}");
        }

        prompt.AppendLine();
        prompt.AppendLine($"Preferencia dietética persistida del usuario: {usuario?.PreferenciaDietetica ?? "Ninguna"}.");
        prompt.AppendLine("Respetá esa restricción en la receta. No incluyas ingredientes prohibidos.");

        var receta = await PedirRecetaAsync(prompt.ToString(), systemInstruction);
        var dificultad = NormalizarDificultad(receta);

        if (esBasico && !LimitesPlan.PuedeUsarDificultad(usuario?.Rol, dificultad))
        {
            prompt.AppendLine();
            prompt.AppendLine(
                $"La receta anterior (\"{receta.Titulo}\") resultó de nivel Difícil. Generá OTRA receta claramente más simple: " +
                $"como máximo {ComplejidadReceta.PasosSugeridosBasico} pasos, {ComplejidadReceta.IngredientesSugeridosBasico} ingredientes " +
                $"y {ComplejidadReceta.MinutosSugeridosBasico} minutos, sin técnicas avanzadas.");

            receta = await PedirRecetaAsync(prompt.ToString(), systemInstruction);
            dificultad = NormalizarDificultad(receta);

            if (!LimitesPlan.PuedeUsarDificultad(usuario?.Rol, dificultad))
            {
                throw new ForbiddenException(LimitesPlan.MensajeRecetaGeneradaDificilBasico);
            }
        }

        return receta;
    }

    private async Task<RecetaGeneradaDto> PedirRecetaAsync(string prompt, string systemInstruction)
    {
        var json = await _asistenteIa.GenerarRespuestaJsonAsync(prompt, systemInstruction);
        var receta = DeserializarReceta(json);
        if (receta is not null)
        {
            receta.Ingredientes ??= [];
            receta.Pasos ??= [];
        }

        if (receta is null || string.IsNullOrWhiteSpace(receta.Titulo) || receta.Ingredientes.Count == 0)
        {
            throw new InvalidOperationException(
                "La IA no devolvió una receta válida. Intentá nuevamente o cargá más ingredientes en tu alacena.");
        }

        return receta;
    }

    public async Task<RecetaGuardadaResponse> GuardarRecetaAsync(int usuarioId, RecetaGeneradaDto receta)
    {
        var resultado = await _unidadDeTrabajo.EjecutarAsync(() => GuardarRecetaEnTransaccionAsync(usuarioId, receta));
        _cache.Remove(RecetaService.CacheCatalogoKey);
        return resultado;
    }

    private async Task<RecetaGuardadaResponse> GuardarRecetaEnTransaccionAsync(int usuarioId, RecetaGeneradaDto receta)
    {
        if (receta is null)
        {
            throw new InvalidOperationException("No se recibió la receta a guardar.");
        }

        receta.Ingredientes ??= [];
        receta.Pasos ??= [];

        await _favoritoService.AsegurarCupoFavoritosAsync(usuarioId);

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        var dificultad = NormalizarDificultad(receta);
        if (!LimitesPlan.PuedeUsarDificultad(usuario?.Rol, dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }

        var titulo = Recortar(GenerarTituloPorDefecto(receta.Titulo, receta.Ingredientes), MaxTitulo);

        var ingredientesValidos = receta.Ingredientes
            .Where(ingrediente => !string.IsNullOrWhiteSpace(ingrediente.Nombre))
            .ToList();
        var pasosValidos = receta.Pasos
            .Where(paso => !string.IsNullOrWhiteSpace(paso))
            .ToList();

        if (ingredientesValidos.Count == 0)
        {
            throw new InvalidOperationException("La receta debe tener al menos un ingrediente.");
        }

        if (pasosValidos.Count == 0)
        {
            throw new InvalidOperationException("La receta debe tener al menos un paso de preparación.");
        }

        var ingredientesReceta = new List<IngredienteReceta>();
        var nombresProcesados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ingrediente in ingredientesValidos)
        {
            var nombre = _normalizer.Normalizar(ingrediente.Nombre);

            if (!nombresProcesados.Add(nombre))
            {
                continue;
            }

            var ingredienteId = await ObtenerOCrearIngredienteAsync(nombre);

            ingredientesReceta.Add(new IngredienteReceta
            {
                IngredienteId = ingredienteId,
                Cantidad = AjustarCantidad(ingrediente.Cantidad),
                UnidadMedida = string.IsNullOrWhiteSpace(ingrediente.UnidadMedida)
                    ? UnidadesMedida.Unidad
                    : Recortar(ingrediente.UnidadMedida, MaxUnidad)
            });
        }

        var pasos = pasosValidos
            .Select((paso, indice) => new PreparacionReceta
            {
                NumeroPaso = indice + 1,
                DescripcionPaso = Recortar(paso, MaxPaso)
            })
            .ToList();

        var nuevaReceta = new Receta
        {
            Titulo = titulo,
            Descripcion = Recortar(receta.Descripcion, MaxDescripcion),
            TiempoPreparacionMinutos = Math.Clamp(receta.TiempoPreparacionMinutos, 1, MaxMinutos),
            Porciones = Math.Clamp(receta.Porciones, 1, MaxPorciones),
            CaloriasEstimadas = Math.Clamp(receta.CaloriasEstimadas, 0, MaxCalorias),
            Dificultad = dificultad,
            Categoria = CategoriasReceta.Normalizar(receta.Categoria),
            UsuarioId = usuarioId,
            IngredientesReceta = ingredientesReceta,
            Preparaciones = pasos
        };

        var recetaCreada = await _recetaRepository.AddAsync(nuevaReceta);

        await _favoritoService.AddFavoritoAsync(new FavoritoCreateDto
        {
            UsuarioId = usuarioId,
            RecetaId = recetaCreada.Id
        });

        return new RecetaGuardadaResponse
        {
            RecetaId = recetaCreada.Id,
            Titulo = recetaCreada.Titulo,
            Favorita = true,
            Mensaje = "Receta guardada y agregada a tus favoritos."
        };
    }

    public async Task AsegurarIngredientesEnCatalogoAsync(RecetaGeneradaDto receta)
    {
        if (receta is null)
        {
            return;
        }

        var nombresProcesados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ingrediente in receta.Ingredientes)
        {
            if (string.IsNullOrWhiteSpace(ingrediente.Nombre))
            {
                continue;
            }

            var nombre = _normalizer.Normalizar(ingrediente.Nombre);
            if (!nombresProcesados.Add(nombre))
            {
                continue;
            }

            await ObtenerOCrearIngredienteAsync(nombre);
        }
    }

    private static string Recortar(string? texto, int maximo)
    {
        var limpio = texto?.Trim() ?? string.Empty;
        return limpio.Length <= maximo ? limpio : limpio[..maximo].TrimEnd();
    }

    private static decimal AjustarCantidad(decimal cantidad) =>
        cantidad <= 0 ? 1m : Math.Min(Math.Round(cantidad, 2), MaxCantidad);

    private async Task<int> ObtenerOCrearIngredienteAsync(string nombre)
    {
        var nombreNormalizado = Recortar(_normalizer.Normalizar(nombre), MaxNombreIngrediente);
        var existente = await _ingredienteRepository.FirstOrDefaultAsync(
            ingrediente => ingrediente.Nombre == nombreNormalizado);

        if (existente is not null)
        {
            return existente.Id;
        }

        var creado = await _ingredienteRepository.AddAsync(new Ingrediente
        {
            Nombre = nombreNormalizado,
            Categoria = "Varios"
        });

        return creado.Id;
    }

    public static string GenerarTituloPorDefecto(string? titulo, IEnumerable<IngredienteGeneradoDto> ingredientes)
    {
        var limpio = titulo?.Trim();
        if (!string.IsNullOrWhiteSpace(limpio) &&
            !limpio.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return limpio;
        }

        var nombres = (ingredientes ?? [])
            .Select(ingrediente => ingrediente.Nombre?.Trim())
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Take(2)
            .ToList();

        return nombres.Count > 0
            ? $"Receta con {string.Join(" y ", nombres)}"
            : "Receta sin título";
    }

    public static DificultadReceta NormalizarDificultad(RecetaGeneradaDto receta)
    {
        receta.Categoria = CategoriasReceta.Normalizar(receta.Categoria);

        var pasos = receta.Pasos.Where(paso => !string.IsNullOrWhiteSpace(paso)).ToList();
        var ingredientes = receta.Ingredientes.Count(ingrediente => !string.IsNullOrWhiteSpace(ingrediente.Nombre));
        var textos = new List<string?> { receta.Titulo, receta.Descripcion };
        textos.AddRange(pasos);

        var efectiva = ComplejidadReceta.Efectiva(
            ComplejidadReceta.Parsear(receta.Dificultad),
            receta.TiempoPreparacionMinutos,
            pasos.Count,
            ingredientes,
            textos);

        receta.Dificultad = efectiva.ToString();
        return efectiva;
    }

    private static RecetaGeneradaDto? DeserializarReceta(string json)
    {
        var limpio = JsonIa.LimpiarBloqueCodigo(json);
        if (limpio.Length == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RecetaGeneradaDto>(limpio, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
