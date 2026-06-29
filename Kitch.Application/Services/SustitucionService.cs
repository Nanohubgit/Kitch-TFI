using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Application.DTOs.Sustituciones;
using Kitch.Application.Interfaces;
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
    private readonly IGeminiClient _geminiClient;

    public SustitucionService(
        IRepository<SustitutoIngrediente> sustitutoRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRepository<StockUsuario> stockRepository,
        IGeminiClient geminiClient)
    {
        _sustitutoRepository = sustitutoRepository;
        _ingredienteRepository = ingredienteRepository;
        _stockRepository = stockRepository;
        _geminiClient = geminiClient;
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

        // Si todavía no hay sustitutos cargados para este ingrediente, los genera la IA y los persiste.
        // Las próximas consultas ya los leen de la base (funciona como caché).
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

        // Lo que el usuario ya tiene en su alacena (con stock > 0) para priorizar.
        var stockUsuario = await _stockRepository.FindAsync(
            stock => stock.UsuarioId == usuarioId && stock.Cantidad > 0);
        var disponiblesEnAlacena = stockUsuario
            .Select(stock => stock.IngredienteId)
            .ToHashSet();

        return sustitutos
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
            // Primero los que el usuario ya tiene; dentro de cada grupo, alfabético.
            .OrderByDescending(sugerido => sugerido.DisponibleEnAlacena)
            .ThenBy(sugerido => sugerido.Nombre)
            .ToList();
    }

    private async Task GenerarYPersistirSustitutosAsync(Ingrediente ingredienteOriginal)
    {
        var prompt = $"Ingrediente original: {ingredienteOriginal.Nombre}.";
        var json = await _geminiClient.GenerarRespuestaJsonAsync(prompt, InstruccionSustitutos);

        var generados = DeserializarSustitutos(json);
        if (generados is null || generados.Count == 0)
        {
            return;
        }

        foreach (var generado in generados)
        {
            var nombre = generado.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                continue;
            }

            // No tiene sentido que un ingrediente sea sustituto de sí mismo.
            if (string.Equals(nombre, ingredienteOriginal.Nombre, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // El sustituto tiene que existir en el catálogo: lo buscamos o lo creamos.
            var ingredienteSustituto = await _ingredienteRepository.FirstOrDefaultAsync(
                ingrediente => ingrediente.Nombre == nombre);

            ingredienteSustituto ??= await _ingredienteRepository.AddAsync(new Ingrediente
            {
                Nombre = nombre,
                Categoria = "Varios"
            });

            // Evitamos duplicar la misma relación original -> sustituto.
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
