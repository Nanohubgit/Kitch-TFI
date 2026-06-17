using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class PlanificadorService : IPlanificadorService
{
    private readonly IRepository<ComidaPlanificada> _repository;

    public PlanificadorService(IRepository<ComidaPlanificada> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ComidaPlanificada>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _repository.FindAsync(comida => comida.UsuarioId == usuarioId);
    }

    public async Task<IEnumerable<ComidaPlanificada>> GetByFechaAsync(int usuarioId, DateTime fecha)
    {
        var fechaInicio = fecha.Date;
        var fechaFin = fechaInicio.AddDays(1);

        return await _repository.FindAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= fechaInicio &&
            comida.FechaAsignada < fechaFin);
    }

    public async Task<ComidaPlanificada?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<ComidaPlanificada> CreateAsync(ComidaPlanificada comida)
    {
        return await _repository.AddAsync(comida);
    }

    public async Task<bool> UpdateAsync(int id, ComidaPlanificada comida)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        comida.Id = id;
        await _repository.UpdateAsync(comida);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var comida = await _repository.GetByIdAsync(id);

        if (comida is null)
        {
            return false;
        }

        await _repository.DeleteAsync(comida);

        return true;
    }
}
