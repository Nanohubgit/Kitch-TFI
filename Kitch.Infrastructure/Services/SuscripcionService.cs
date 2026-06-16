using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class SuscripcionService : ISuscripcionService
{
    private readonly KitchDbContext _context;

    public SuscripcionService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Suscripcion>> GetAllAsync()
    {
        return await _context.Suscripciones.ToListAsync();
    }

    public async Task<Suscripcion?> GetByIdAsync(int id)
    {
        return await _context.Suscripciones.FindAsync(id);
    }

    public async Task<Suscripcion> CreateAsync(Suscripcion suscripcion)
    {
        await _context.Suscripciones.AddAsync(suscripcion);
        await _context.SaveChangesAsync();

        return suscripcion;
    }

    public async Task<bool> UpdateAsync(int id, Suscripcion suscripcion)
    {
        var existingSuscripcion = await _context.Suscripciones.FindAsync(id);

        if (existingSuscripcion is null)
        {
            return false;
        }

        suscripcion.Id = id;
        _context.Entry(existingSuscripcion).CurrentValues.SetValues(suscripcion);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var suscripcion = await _context.Suscripciones.FindAsync(id);

        if (suscripcion is null)
        {
            return false;
        }

        _context.Suscripciones.Remove(suscripcion);
        await _context.SaveChangesAsync();

        return true;
    }
}
