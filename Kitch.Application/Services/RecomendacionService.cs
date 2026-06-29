using Kitch.Application.DTOs.Recomendacion;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class RecomendacionService : IRecomendacionService
{
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;

    public RecomendacionService(
        IRepository<Receta> recetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository)
    {
        _recetaRepository = recetaRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
    }

    public async Task<IEnumerable<RecetaCompatibleDto>> RecomendarAsync(int usuarioId, int? maxFaltantes = null)
    {
        // 1. Qué ingredientes tiene el usuario en su alacena (con stock > 0).
        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId && item.Cantidad > 0);
        var idsEnAlacena = stock.Select(item => item.IngredienteId).ToHashSet();

        // 2. Todas las recetas con sus ingredientes.
        var recetas = await _recetaRepository.FindWithIncludesAsync(
            receta => true,
            receta => receta.IngredientesReceta);

        // 3. Nombres de los ingredientes (para mostrar los faltantes con su nombre real).
        var ingredienteIds = recetas
            .SelectMany(receta => receta.IngredientesReceta)
            .Select(ingrediente => ingrediente.IngredienteId)
            .Distinct()
            .ToList();

        var ingredientes = await _ingredienteRepository.FindAsync(
            ingrediente => ingredienteIds.Contains(ingrediente.Id));
        var nombrePorIngrediente = ingredientes.ToDictionary(
            ingrediente => ingrediente.Id,
            ingrediente => ingrediente.Nombre);

        // 4. Para cada receta, calculamos cuánto cubre la alacena y qué falta.
        var recomendaciones = new List<RecetaCompatibleDto>();

        foreach (var receta in recetas)
        {
            var total = receta.IngredientesReceta.Count;
            if (total == 0)
            {
                continue;
            }

            var faltantes = receta.IngredientesReceta
                .Where(ingrediente => !idsEnAlacena.Contains(ingrediente.IngredienteId))
                .Select(ingrediente => nombrePorIngrediente.TryGetValue(ingrediente.IngredienteId, out var nombre)
                    ? nombre
                    : $"Ingrediente #{ingrediente.IngredienteId}")
                .ToList();

            // Si se pidió un tope de faltantes, descartamos las que se pasen.
            if (maxFaltantes.HasValue && faltantes.Count > maxFaltantes.Value)
            {
                continue;
            }

            var disponibles = total - faltantes.Count;

            recomendaciones.Add(new RecetaCompatibleDto
            {
                RecetaId = receta.Id,
                Titulo = receta.Titulo,
                Dificultad = receta.Dificultad,
                TiempoPreparacionMinutos = receta.TiempoPreparacionMinutos,
                CaloriasEstimadas = receta.CaloriasEstimadas,
                Porciones = receta.Porciones,
                TotalIngredientes = total,
                IngredientesDisponibles = disponibles,
                PorcentajeCoincidencia = (int)Math.Round(disponibles * 100.0 / total),
                IngredientesFaltantes = faltantes
            });
        }

        // 5. Priorizamos: mayor coincidencia, menos faltantes, y más rápido de preparar.
        return recomendaciones
            .OrderByDescending(receta => receta.PorcentajeCoincidencia)
            .ThenBy(receta => receta.IngredientesFaltantes.Count)
            .ThenBy(receta => receta.TiempoPreparacionMinutos)
            .ToList();
    }
}
