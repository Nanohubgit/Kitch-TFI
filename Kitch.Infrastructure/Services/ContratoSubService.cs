using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class ContratoSubService : IContratoSubService
{
    private readonly KitchDbContext _context;

    public ContratoSubService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ContratoSub>> GetAllAsync()
    {
        return await _context.ContratosSub.ToListAsync();
    }

    public async Task<IEnumerable<ContratoSub>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _context.ContratosSub
            .Where(contratoSub => contratoSub.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<ContratoSub?> GetByIdAsync(int id)
    {
        return await _context.ContratosSub.FindAsync(id);
    }

    public async Task<ContratoSub> CreateAsync(ContratoSub contratoSub)
    {
        await _context.ContratosSub.AddAsync(contratoSub);
        await _context.SaveChangesAsync();

        return contratoSub;
    }

    public async Task<bool> UpdateAsync(int id, ContratoSub contratoSub)
    {
        var existingContratoSub = await _context.ContratosSub.FindAsync(id);

        if (existingContratoSub is null)
        {
            return false;
        }

        contratoSub.Id = id;
        _context.Entry(existingContratoSub).CurrentValues.SetValues(contratoSub);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var contratoSub = await _context.ContratosSub.FindAsync(id);

        if (contratoSub is null)
        {
            return false;
        }

        _context.ContratosSub.Remove(contratoSub);
        await _context.SaveChangesAsync();

        return true;
    }
}
