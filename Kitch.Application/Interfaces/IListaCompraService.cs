using Kitch.Application.DTOs.ListaCompra;

namespace Kitch.Application.Interfaces;

public interface IListaCompraService
{
    Task<IEnumerable<ItemListaCompraResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<ItemListaCompraResponseDto?> GetByIdAsync(int id);
    Task<ItemListaCompraResponseDto> CreateAsync(ItemListaCompraCreateDto item);
    Task<bool> UpdateAsync(int id, ItemListaCompraUpdateDto item);
    Task<bool> DeleteAsync(int id);
    Task<bool> MarcarComoCompradoAsync(int id);
    Task<IEnumerable<ItemListaCompraResponseDto>> GenerarListaFaltantesAsync(int usuarioId);
}
