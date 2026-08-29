using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Application.DTOs.Sustituciones;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class SustitucionService : ISustitucionService
{
    private const string InstruccionSustitutos =
        "Sos un chef experto. Te paso un ingrediente y tenés que dar hasta 5 sustitutos culinarios " +
        "viables y comunes. El 'factorEquivalencia' es cuánto del sustituto equivale a 1 unidad del " +
        "original (ej: 0.8 => usar 0.8 por cada 1 del original). Respondé ÚNICAMENTE con JSON válido, " +
        "sin markdown, con esta forma: {\"sustitutos\":[{\"nombre\":string,\"factorEquivalencia\":number,\"notas\":string}]}.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IRepository<SustitutoIngrediente> _sustitutoRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IAsistenteIaClient _asistenteIa;
    private readonly IIngredienteNormalizerService _normalizer;

    public SustitucionService(
        IRepository<SustitutoIngrediente> sustitutoRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Usuario> usuarioRepository,
        IAsistenteIaClient asistenteIa,
        IIngredienteNormalizerService normalizer)
    {
        _sustitutoRepository = sustitutoRepository;
        _ingredienteRepository = ingredienteRepository;
        _stockRepository = stockRepository;
        _usuarioRepository = usuarioRepository;
        _asistenteIa = asistenteIa;
        _normalizer = normalizer;
    }

    public async Task<IEnumerable<SustitutoSugerido>> BuscarSustitutosAsync(int usuarioId, int ingredienteId)
    {
        var ingredienteOriginal = await _ingredienteRepository.GetByIdAsync(ingredienteId);
        if (ingredienteOriginal is null)
        {
            throw new InvalidOperationException("El ingrediente no existe.");
        }

        var sustitutos = await _sustitutoRepository.FindAsync(
            sustituto => sustituto.IngredienteOriginalId == ingredienteId);

        if (sustitutos.Count == 0)
        {
            await GenerarYPersistirSustitutosAsync(ingredienteOriginal);
            sustitutos = await _sustitutoRepository.FindAsync(
                sustituto => sustituto.IngredienteOriginalId == ingredienteId);
        }

        if (sustitutos.Count == 0)
        {
            return Enumerable.Empty<SustitutoSugerido>();
        }

        var sustitutoIds = sustitutos
            .Select(sustituto => sustituto.IngredienteSustitutoId)
            .Distinct()
            .ToList();

        var ingredientes = await _ingredienteRepository.FindAsync(
            ingrediente => sustitutoIds.Contains(ingrediente.Id));
        var nombrePorIngrediente = ingredientes.ToDictionary(
            ingrediente => ingrediente.Id,
            ingrediente => ingrediente.Nombre);

        var stockUsuario = await _stockRepository.FindAsync(
            stock => stock.UsuarioId == usuarioId && stock.Cantidad > 0);
        var disponiblesEnAlacena = stockUsuario
            .Select(stock => stock.IngredienteId)
            .ToHashSet();

        var sugeridos = sustitutos
            .Select(sustituto => new SustitutoSugerido
            {
                IngredienteId = sustituto.IngredienteSustitutoId,
                Nombre = nombrePorIngrediente.TryGetValue(sustituto.IngredienteSustitutoId, out var nombre)
                    ? nombre
                    : $"Ingrediente #{sustituto.IngredienteSustitutoId}",
                FactorEquivalencia = sustituto.FactorEquivalencia,
                Notas = sustituto.Notas,
                DisponibleEnAlacena = disponiblesEnAlacena.Contains(sustituto.IngredienteSustitutoId)
            })
            .OrderByDescending(sugerido => sugerido.DisponibleEnAlacena)
            .ThenBy(sugerido => sugerido.Nombre)
            .ToList();

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is not null && !RolUsuario.TieneAccesoPremium(usuario.Rol))
        {
            return sugeridos.Take(LimitesPlan.MaxSustitutosBasico).ToList();
        }

        return sugeridos;
    }

    private async Task GenerarYPersistirSustitutosAsync(Ingrediente ingredienteOriginal)
    {
        var prompt = $"Ingrediente original: {ingredienteOriginal.Nombre}.";
        var json = await _asistenteIa.GenerarRespuestaJsonAsync(prompt, InstruccionSustitutos);

        var generados = DeserializarSustitutos(json);
        if (generados is null || generados.Count == 0)
        {
            return;
        }

        var nombreOriginal = _normalizer.Normalizar(ingredienteOriginal.Nombre);

        foreach (var generado in generados)
        {
            if (string.IsNullOrWhiteSpace(generado.Nombre))
            {
                continue;
            }

            var nombre = _normalizer.Normalizar(generado.Nombre);

            if (string.Equals(nombre, nombreOriginal, StringComparison.Ordinal))
            {
                continue;
            }

            var ingredienteSustituto = await _ingredienteRepository.FirstOrDefaultAsync(
                ingrediente => ingrediente.Nombre == nombre);

            ingredienteSustituto ??= await _ingredienteRepository.AddAsync(new Ingrediente
            {
                Nombre = nombre,
                Categoria = "Varios"
            });

            var yaExiste = await _sustitutoRepository.AnyAsync(sustituto =>
                sustituto.IngredienteOriginalId == ingredienteOriginal.Id &&
                sustituto.IngredienteSustitutoId == ingredienteSustituto.Id);

            if (yaExiste)
            {
                continue;
            }

            await _sustitutoRepository.AddAsync(new SustitutoIngrediente
            {
                IngredienteOriginalId = ingredienteOriginal.Id,
                IngredienteSustitutoId = ingredienteSustituto.Id,
                FactorEquivalencia = generado.FactorEquivalencia > 0 ? generado.FactorEquivalencia : 1m,
                Notas = generado.Notas
            });
        }
    }

    private static List<SustitutoGeneradoDto>? DeserializarSustitutos(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var limpio = json.Trim();
        if (limpio.StartsWith("```"))
        {
            limpio = limpio.Trim('`');
            var saltoLinea = limpio.IndexOf('\n');
            if (saltoLinea >= 0 && limpio[..saltoLinea].Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                limpio = limpio[(saltoLinea + 1)..];
            }
        }

        try
        {
            return JsonSerializer.Deserialize<SustitutosGeneradosDto>(limpio, JsonOptions)?.Sustitutos;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class SustitutosGeneradosDto
    {
        [JsonPropertyName("sustitutos")]
        public List<SustitutoGeneradoDto> Sustitutos { get; set; } = new();
    }

    private sealed class SustitutoGeneradoDto
    {
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("factorEquivalencia")]
        public decimal FactorEquivalencia { get; set; } = 1m;

        [JsonPropertyName("notas")]
        public string? Notas { get; set; }
    }
}
