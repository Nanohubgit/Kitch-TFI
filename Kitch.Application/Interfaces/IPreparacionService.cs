using Kitch.Application.DTOs.Preparacion;

namespace Kitch.Application.Interfaces;

public interface IPreparacionService
{
    Task<PrevisualizarPorcionesResponseDto> PrevisualizarRecalculoPorcionesAsync(PrevisualizarPorcionesRequestDto request);
    Task<PrevisualizarDescuentoStockResponseDto> PrevisualizarDescuentoStockAsync(PrevisualizarDescuentoStockRequestDto request);
    // usuarioId es necesario para ubicar el stock del usuario que cocina la receta.
    Task DescontarIngredientesAsync(int usuarioId, int recetaId, int porciones);
}
