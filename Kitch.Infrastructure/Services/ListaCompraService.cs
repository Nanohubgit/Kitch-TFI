using Kitch.Application.DTOs.ListaCompra;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
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

    public async Task<IEnumerable<ItemListaCompraResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var items = await _repository.FindAsync(item => item.UsuarioId == usuarioId);
        return items.Select(item => item.ToResponseDto());
    }

    public async Task<ItemListaCompraResponseDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item?.ToResponseDto();
    }

    public async Task<ItemListaCompraResponseDto> CreateAsync(ItemListaCompraCreateDto item)
    {
        var entity = new ItemListaCompra
        {
            UsuarioId = item.UsuarioId,
            NombreArticulo = item.NombreArticulo.Trim(),
            CantidadFaltante = item.CantidadFaltante,
            EstaComprado = false
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, ItemListaCompraUpdateDto item)
    {
        var existingItem = await _repository.GetByIdAsync(id);

        if (existingItem is null)
        {
            return false;
        }

        existingItem.NombreArticulo = item.NombreArticulo.Trim();
        existingItem.CantidadFaltante = item.CantidadFaltante;
        existingItem.EstaComprado = item.EstaComprado;

        await _repository.UpdateAsync(existingItem);

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
