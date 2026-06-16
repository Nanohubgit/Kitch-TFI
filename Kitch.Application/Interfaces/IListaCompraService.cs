using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IListaCompraService
{
    Task<IEnumerable<ItemListaCompra>> GetByUsuarioIdAsync(int usuarioId);
    Task<ItemListaCompra?> GetByIdAsync(int id);
    Task<ItemListaCompra> CreateAsync(ItemListaCompra item);
    Task<bool> UpdateAsync(int id, ItemListaCompra item);
    Task<bool> DeleteAsync(int id);
    Task<bool> MarcarComoCompradoAsync(int id);
}
