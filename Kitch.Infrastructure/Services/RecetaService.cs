using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class RecetaService : IRecetaService
{
    private readonly KitchDbContext _context;

    public RecetaService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Receta>> GetAllAsync()
    {
        return await _context.Recetas.ToListAsync();
    }

    public async Task<Receta?> GetByIdAsync(int id)
    {
        return await _context.Recetas.FindAsync(id);
    }

    public async Task<Receta> CreateAsync(Receta receta)
    {
        await _context.Recetas.AddAsync(receta);
        await _context.SaveChangesAsync();

        return receta;
    }

    public async Task<bool> UpdateAsync(int id, Receta receta)
    {
        var existingReceta = await _context.Recetas.FindAsync(id);

        if (existingReceta is null)
        {
            return false;
        }

        receta.Id = id;
        _context.Entry(existingReceta).CurrentValues.SetValues(receta);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var receta = await _context.Recetas.FindAsync(id);

        if (receta is null)
        {
            return false;
        }

        _context.Recetas.Remove(receta);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<Receta>> GetByDificultadAsync(DificultadReceta dificultad)
    {
        return await _context.Recetas
            .Where(receta => receta.Dificultad == dificultad)
            .ToListAsync();
    }
}
