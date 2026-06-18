using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class PreparacionService : IPreparacionService
{
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<IngredienteReceta> _ingredienteRecetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;

    public PreparacionService(
        IRepository<Receta> recetaRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository)
    {
        _recetaRepository = recetaRepository;
        _ingredienteRecetaRepository = ingredienteRecetaRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
    }

    public async Task DescontarIngredientesAsync(int usuarioId, int recetaId, int porciones)
    {
        if (porciones <= 0)
        {
            throw new ArgumentException("Las porciones deben ser mayores a cero.", nameof(porciones));
        }

        var receta = await _recetaRepository.GetByIdAsync(recetaId)
            ?? throw new InvalidOperationException("La receta no existe.");

        var ingredientesReceta = await _ingredienteRecetaRepository.FindAsync(
            ingrediente => ingrediente.RecetaId == recetaId);

        if (ingredientesReceta.Count == 0)
        {
            throw new InvalidOperationException("La receta no tiene ingredientes cargados.");
        }

        // La receta declara una cantidad de ingredientes para "receta.Porciones" porciones base.
        // Escalamos a las porciones que el usuario realmente va a cocinar.
        var porcionesBase = receta.Porciones > 0 ? receta.Porciones : 1;
        var factor = (decimal)porciones / porcionesBase;

        var stockUsuario = await _stockRepository.FindAsync(stock => stock.UsuarioId == usuarioId);
        var stockPorIngrediente = stockUsuario.ToDictionary(stock => stock.IngredienteId);

        // Primero validamos que alcance para TODOS los ingredientes, así no descontamos a medias.
        var descuentos = new List<(StockUsuario Stock, decimal Cantidad)>();

        foreach (var ingrediente in ingredientesReceta)
        {
            var cantidadADescontar = ingrediente.Cantidad * factor;

            if (!stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var stock))
            {
                throw new InvalidOperationException(
                    $"No hay stock cargado del ingrediente '{await ObtenerNombreIngredienteAsync(ingrediente.IngredienteId)}'.");
            }

            if (stock.Cantidad < cantidadADescontar)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente del ingrediente '{await ObtenerNombreIngredienteAsync(ingrediente.IngredienteId)}'. " +
                    $"Necesario: {cantidadADescontar}, disponible: {stock.Cantidad}.");
            }

            descuentos.Add((stock, cantidadADescontar));
        }

        foreach (var (stock, cantidad) in descuentos)
        {
            stock.Cantidad -= cantidad;
            await _stockRepository.UpdateAsync(stock);
        }
    }

    private async Task<string> ObtenerNombreIngredienteAsync(int ingredienteId)
    {
        var ingrediente = await _ingredienteRepository.GetByIdAsync(ingredienteId);
        return ingrediente?.Nombre ?? $"#{ingredienteId}";
    }
}
