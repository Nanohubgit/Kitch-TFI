using Kitch.Application.DTOs.ListaCompra;
using Kitch.Application.DTOs.Preparacion;

namespace Kitch.Application.Interfaces;

public interface IPreparacionService
{
    Task<PrevisualizarPorcionesResponseDto> PrevisualizarRecalculoPorcionesAsync(PrevisualizarPorcionesRequestDto request);
    Task<IEnumerable<ItemListaCompraResponseDto>> AgregarFaltantesPorcionesAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones = null);
    Task<PrevisualizarDescuentoStockResponseDto> PrevisualizarDescuentoStockAsync(PrevisualizarDescuentoStockRequestDto request);
    Task<DescuentoStockResultadoDto> DescontarIngredientesAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<CantidadUtilizadaDto>? cantidadesUtilizadas = null,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones = null);
    Task<DescuentoStockResultadoDto> DescontarIngredientesParcialAsync(
        int usuarioId,
        int recetaId,
        int porciones,
        IReadOnlyList<SustitucionAplicadaDto>? sustituciones = null);
    Task<AplicarSustitutoResponseDto> AplicarSustitutoAsync(int usuarioId, AplicarSustitutoRequestDto request);
}
