using Kitch.Application.DTOs.Preparacion;

namespace Kitch.Application.Interfaces;

public interface IPreparacionService
{
    Task<PrevisualizarPorcionesResponseDto> PrevisualizarRecalculoPorcionesAsync(PrevisualizarPorcionesRequestDto request);
    Task<PrevisualizarDescuentoStockResponseDto> PrevisualizarDescuentoStockAsync(PrevisualizarDescuentoStockRequestDto request);
    Task DescontarIngredientesAsync(int usuarioId, int recetaId, int porciones);

    // Descuento PARCIAL: resta lo que haya en la alacena (clamp en 0) y devuelve qué se
    // descontó y qué faltó, sin fallar por stock insuficiente. Usado por el chat ("cociné esto").
    // Si porciones <= 0 se asume que se cocinó la receta completa (porciones base).
    Task<DescuentoStockResultadoDto> DescontarIngredientesParcialAsync(int usuarioId, int recetaId, int porciones);
}
