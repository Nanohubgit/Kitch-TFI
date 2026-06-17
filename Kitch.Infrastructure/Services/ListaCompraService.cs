using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class ListaCompraService : IListaCompraService
{
    private readonly IRepository<ItemListaCompra> _repository;

    public ListaCompraService(IRepository<ItemListaCompra> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ItemListaCompra>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _repository.FindAsync(item => item.UsuarioId == usuarioId);
    }

    public async Task<ItemListaCompra?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<ItemListaCompra> CreateAsync(ItemListaCompra item)
    {
        return await _repository.AddAsync(item);
    }

    public async Task<bool> UpdateAsync(int id, ItemListaCompra item)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        item.Id = id;
        await _repository.UpdateAsync(item);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);

        if (item is null)
        {
            return false;
        }

        await _repository.DeleteAsync(item);

        return true;
    }

    public async Task<bool> MarcarComoCompradoAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);

        if (item is null)
        {
            return false;
        }

        item.EstaComprado = true;
        await _repository.UpdateAsync(item);

        return true;
    }
}
