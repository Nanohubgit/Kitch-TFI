using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class PlanificadorService : IPlanificadorService
{
    private readonly KitchDbContext _context;

    public PlanificadorService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ComidaPlanificada>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _context.ComidasPlanificadas
            .Where(comida => comida.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<IEnumerable<ComidaPlanificada>> GetByFechaAsync(int usuarioId, DateTime fecha)
    {
        var fechaInicio = fecha.Date;
        var fechaFin = fechaInicio.AddDays(1);

        return await _context.ComidasPlanificadas
            .Where(comida =>
                comida.UsuarioId == usuarioId &&
                comida.FechaAsignada >= fechaInicio &&
                comida.FechaAsignada < fechaFin)
            .ToListAsync();
    }

    public async Task<ComidaPlanificada?> GetByIdAsync(int id)
    {
        return await _context.ComidasPlanificadas.FindAsync(id);
    }

    public async Task<ComidaPlanificada> CreateAsync(ComidaPlanificada comida)
    {
        await _context.ComidasPlanificadas.AddAsync(comida);
        await _context.SaveChangesAsync();

        return comida;
    }

    public async Task<bool> UpdateAsync(int id, ComidaPlanificada comida)
    {
        var existingComida = await _context.ComidasPlanificadas.FindAsync(id);

        if (existingComida is null)
        {
            return false;
        }

        comida.Id = id;
        _context.Entry(existingComida).CurrentValues.SetValues(comida);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var comida = await _context.ComidasPlanificadas.FindAsync(id);

        if (comida is null)
        {
            return false;
        }

        _context.ComidasPlanificadas.Remove(comida);
        await _context.SaveChangesAsync();

        return true;
    }
}
