using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class ListaCompraService : IListaCompraService
{
    private readonly KitchDbContext _context;

    public ListaCompraService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ItemListaCompra>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _context.ItemsListaCompra
            .Where(item => item.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<ItemListaCompra?> GetByIdAsync(int id)
    {
        return await _context.ItemsListaCompra.FindAsync(id);
    }

    public async Task<ItemListaCompra> CreateAsync(ItemListaCompra item)
    {
        await _context.ItemsListaCompra.AddAsync(item);
        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<bool> UpdateAsync(int id, ItemListaCompra item)
    {
        var existingItem = await _context.ItemsListaCompra.FindAsync(id);

        if (existingItem is null)
        {
            return false;
        }

        item.Id = id;
        _context.Entry(existingItem).CurrentValues.SetValues(item);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _context.ItemsListaCompra.FindAsync(id);

        if (item is null)
        {
            return false;
        }

        _context.ItemsListaCompra.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarcarComoCompradoAsync(int id)
    {
        var item = await _context.ItemsListaCompra.FindAsync(id);

        if (item is null)
        {
            return false;
        }

        item.EstaComprado = true;
        await _context.SaveChangesAsync();

        return true;
    }
}
