using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class PagoService : IPagoService
{
    private readonly KitchDbContext _context;

    public PagoService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Pago>> GetAllAsync()
    {
        return await _context.Pagos.ToListAsync();
    }

    public async Task<IEnumerable<Pago>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _context.Pagos
            .Where(pago => pago.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<Pago?> GetByIdAsync(int id)
    {
        return await _context.Pagos.FindAsync(id);
    }

    public async Task<Pago> CreateAsync(Pago pago)
    {
        await _context.Pagos.AddAsync(pago);
        await _context.SaveChangesAsync();

        return pago;
    }

    public async Task<bool> UpdateAsync(int id, Pago pago)
    {
        var existingPago = await _context.Pagos.FindAsync(id);

        if (existingPago is null)
        {
            return false;
        }

        pago.Id = id;
        _context.Entry(existingPago).CurrentValues.SetValues(pago);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var pago = await _context.Pagos.FindAsync(id);

        if (pago is null)
        {
            return false;
        }

        _context.Pagos.Remove(pago);
        await _context.SaveChangesAsync();

        return true;
    }
}
