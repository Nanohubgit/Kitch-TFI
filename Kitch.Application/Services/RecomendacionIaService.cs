using System.Text;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kitch.Application.Services;

public class RecomendacionIaService : IRecomendacionIaService
{
    // Limitamos cuántas recetas mandamos a la IA para no inflar el prompt (y el costo).
    private const int MaxRecetasEnContexto = 8;

    private const string InstruccionPorDefecto =
        "Sos 'Kitch-AI', el asistente de cocina exclusivo de la plataforma Kitch. " +
        "Te paso el stock real de la alacena del usuario y una lista de recetas con su porcentaje " +
        "de coincidencia de ingredientes y los ingredientes que le faltan. " +
        "Tu tarea es recomendar qué cocinar priorizando: (1) mayor coincidencia de ingredientes, " +
        "(2) las preferencias o restricciones que indique el usuario, y (3) menor dificultad y tiempo. " +
        "Para cada receta sugerida aclará SIEMPRE qué ingredientes le faltan (si faltan). " +
        "Podés sugerir recetas aunque falten ingredientes, pero priorizá las que falten pocos. " +
        "Respondé únicamente sobre cocina, de forma clara y concisa, en español. " +
        "No inventes recetas ni ingredientes que no estén en los datos que te paso.";

    private readonly IGeminiClient _geminiClient;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IConfiguration _configuration;

    public RecomendacionIaService(
        IGeminiClient geminiClient,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRepository<Receta> recetaRepository,
        IConfiguration configuration)
    {
        _geminiClient = geminiClient;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
        _recetaRepository = recetaRepository;
        _configuration = configuration;
    }

    public async Task<string> RecomendarRecetasAsync(int usuarioId, string? preferencias = null)
    {
        // 1. Stock real del usuario (solo lo que tiene cantidad disponible).
        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId && item.Cantidad > 0);
        var ingredientesDisponibles = stock.Select(item => item.IngredienteId).ToHashSet();

        // 2. Diccionario de nombres de ingredientes (para no exponer IDs a la IA).
        var ingredientes = await _ingredienteRepository.GetAllAsync();
        var nombrePorIngrediente = ingredientes.ToDictionary(
            ingrediente => ingrediente.Id,
            ingrediente => ingrediente.Nombre);

        // 3. Recetas con sus ingredientes, para cruzar contra el stock.
        var recetas = await _recetaRepository.FindWithIncludesAsync(
            receta => true,
            receta => receta.IngredientesReceta);

        // 4. Cálculo determinista del % de coincidencia y de los faltantes por receta.
        var analisis = recetas
            .Select(receta =>
            {
                var total = receta.IngredientesReceta.Count;

                var faltantes = receta.IngredientesReceta
                    .Where(ingrediente => !ingredientesDisponibles.Contains(ingrediente.IngredienteId))
                    .Select(ingrediente => nombrePorIngrediente.TryGetValue(ingrediente.IngredienteId, out var nombre)
                        ? nombre
                        : $"Ingrediente #{ingrediente.IngredienteId}")
                    .ToList();

                var disponibles = total - faltantes.Count;
                var coincidencia = total == 0 ? 0 : (int)Math.Round(disponibles * 100.0 / total);

                return new RecetaAnalizada(receta, total, faltantes, coincidencia);
            })
            // Mejor coincidencia primero; a igualdad, lo más fácil y rápido.
            .OrderByDescending(item => item.Coincidencia)
            .ThenBy(item => item.Receta.Dificultad)
            .ThenBy(item => item.Receta.TiempoPreparacionMinutos)
            .Take(MaxRecetasEnContexto)
            .ToList();

        var contexto = ConstruirContexto(stock, nombrePorIngrediente, analisis);

        var instruccion = _configuration["Gemini:RecommendationInstruction"];
        if (string.IsNullOrWhiteSpace(instruccion))
        {
            instruccion = InstruccionPorDefecto;
        }

        var prompt = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(preferencias))
        {
            prompt.AppendLine($"Preferencias o restricciones del usuario: {preferencias}");
            prompt.AppendLine();
        }

        prompt.AppendLine("Con estos datos, recomendame qué cocinar y qué me conviene comprar:");
        prompt.AppendLine();
        prompt.Append(contexto);

        return await _geminiClient.GenerarRespuestaAsync(prompt.ToString(), instruccion);
    }

    private static string ConstruirContexto(
        IReadOnlyCollection<StockUsuario> stock,
        IReadOnlyDictionary<int, string> nombrePorIngrediente,
        IReadOnlyCollection<RecetaAnalizada> analisis)
    {
        var contexto = new StringBuilder();

        contexto.AppendLine("STOCK DEL USUARIO (alacena virtual):");
        if (stock.Count == 0)
        {
            contexto.AppendLine("- (vacío: el usuario todavía no cargó ingredientes)");
        }
        else
        {
            foreach (var item in stock)
            {
                var nombre = nombrePorIngrediente.TryGetValue(item.IngredienteId, out var encontrado)
                    ? encontrado
                    : $"Ingrediente #{item.IngredienteId}";
                contexto.AppendLine($"- {nombre}: {item.Cantidad} {item.UnidadMedida}");
            }
        }

        contexto.AppendLine();
        contexto.AppendLine("RECETAS DISPONIBLES (ordenadas por coincidencia):");
        if (analisis.Count == 0)
        {
            contexto.AppendLine("- (no hay recetas cargadas en el sistema)");
        }
        else
        {
            foreach (var item in analisis)
            {
                var faltantesTexto = item.Faltantes.Count == 0
                    ? "ninguno (tenés todo)"
                    : string.Join(", ", item.Faltantes);

                contexto.AppendLine(
                    $"- {item.Receta.Titulo} | dificultad: {item.Receta.Dificultad} | " +
                    $"{item.Receta.TiempoPreparacionMinutos} min | {item.Receta.CaloriasEstimadas} kcal | " +
                    $"{item.Receta.Porciones} porciones | coincidencia: {item.Coincidencia}% | faltan: {faltantesTexto}");
            }
        }

        return contexto.ToString();
    }

    private sealed record RecetaAnalizada(
        Receta Receta,
        int TotalIngredientes,
        IReadOnlyList<string> Faltantes,
        int Coincidencia);
}
