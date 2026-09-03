using Kitch.Application.DTOs.Recomendacion;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class RecomendacionService : IRecomendacionService
{
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRepository<Usuario> _usuarioRepository;

    public RecomendacionService(
        IRepository<Receta> recetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRepository<Usuario> usuarioRepository)
    {
        _recetaRepository = recetaRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IEnumerable<RecetaCompatibleDto>> RecomendarAsync(int usuarioId, int? maxFaltantes = null)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        var rol = usuario?.Rol;

        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId && item.Cantidad > 0);
        var idsEnAlacena = stock.Select(item => item.IngredienteId).ToHashSet();

        var recetas = await _recetaRepository.FindWithIncludesAsync(
            receta => true,
            receta => receta.IngredientesReceta);

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

        var recomendaciones = new List<RecetaCompatibleDto>();

        foreach (var receta in recetas)
        {
            if (!LimitesPlan.PuedeUsarDificultad(rol, receta.Dificultad))
            {
                continue;
            }

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
                Categoria = receta.Categoria,
                TotalIngredientes = total,
                IngredientesDisponibles = disponibles,
                PorcentajeCoincidencia = (int)Math.Round(disponibles * 100.0 / total),
                IngredientesFaltantes = faltantes
            });
        }

        return recomendaciones
            .OrderByDescending(receta => receta.PorcentajeCoincidencia)
            .ThenBy(receta => receta.IngredientesFaltantes.Count)
            .ThenBy(receta => receta.TiempoPreparacionMinutos)
            .ToList();
    }
}
