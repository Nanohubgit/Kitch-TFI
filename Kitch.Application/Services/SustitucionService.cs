using Kitch.Application.DTOs.Sustituciones;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class SustitucionService : ISustitucionService
{
    private readonly IRepository<SustitutoIngrediente> _sustitutoRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRepository<StockUsuario> _stockRepository;

    public SustitucionService(
        IRepository<SustitutoIngrediente> sustitutoRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRepository<StockUsuario> stockRepository)
    {
        _sustitutoRepository = sustitutoRepository;
        _ingredienteRepository = ingredienteRepository;
        _stockRepository = stockRepository;
    }

    public async Task<IEnumerable<SustitutoSugerido>> BuscarSustitutosAsync(int usuarioId, int ingredienteId)
    {
        if (!await _ingredienteRepository.AnyAsync(ingrediente => ingrediente.Id == ingredienteId))
        {
            throw new InvalidOperationException("El ingrediente no existe.");
        }

        var sustitutos = await _sustitutoRepository.FindAsync(
            sustituto => sustituto.IngredienteOriginalId == ingredienteId);

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
}
