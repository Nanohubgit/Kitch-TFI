using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class StockUsuarioService : IStockUsuarioService
{
    private readonly KitchDbContext _context;

    public StockUsuarioService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StockUsuario>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _context.StockUsuarios
            .Where(stock => stock.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<StockUsuario?> GetByIdAsync(int id)
    {
        return await _context.StockUsuarios.FindAsync(id);
    }

    public async Task<StockUsuario> CreateAsync(StockUsuario stock)
    {
        await _context.StockUsuarios.AddAsync(stock);
        await _context.SaveChangesAsync();

        return stock;
    }

    public async Task<bool> UpdateAsync(int id, StockUsuario stock)
    {
        var existingStock = await _context.StockUsuarios.FindAsync(id);

        if (existingStock is null)
        {
            return false;
        }

        stock.Id = id;
        _context.Entry(existingStock).CurrentValues.SetValues(stock);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var stock = await _context.StockUsuarios.FindAsync(id);

        if (stock is null)
        {
            return false;
        }

        _context.StockUsuarios.Remove(stock);
        await _context.SaveChangesAsync();

        return true;
    }
}
