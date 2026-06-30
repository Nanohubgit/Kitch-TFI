using Kitch.Application.DTOs.Preparacion;

namespace Kitch.Application.Interfaces;

public interface IPreparacionService
{
    Task<PrevisualizarPorcionesResponseDto> PrevisualizarRecalculoPorcionesAsync(PrevisualizarPorcionesRequestDto request);
    Task<PrevisualizarDescuentoStockResponseDto> PrevisualizarDescuentoStockAsync(PrevisualizarDescuentoStockRequestDto request);
    Task DescontarIngredientesAsync(int usuarioId, int recetaId, int porciones);
}
