using Kitch.Application.DTOs.ListaCompra;

namespace Kitch.Application.Interfaces;

public interface IListaCompraService
{
    Task<IEnumerable<ItemListaCompraResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    // Las operaciones sobre un ítem reciben el usuarioId del token para verificar
    // que el ítem le pertenezca (evita IDOR: ver/editar/borrar ítems de otro usuario).
    Task<ItemListaCompraResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<ItemListaCompraResponseDto> CreateAsync(ItemListaCompraCreateDto item);
    Task<bool> UpdateAsync(int id, ItemListaCompraUpdateDto item, int usuarioId);
    Task<bool> DeleteAsync(int id, int usuarioId);
    Task<bool> MarcarComoCompradoAsync(int id, int usuarioId);
    Task<IEnumerable<ItemListaCompraResponseDto>> GenerarListaFaltantesAsync(int usuarioId);
}
