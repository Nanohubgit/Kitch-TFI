using Kitch.Application.DTOs.Planificador;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
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

    public async Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var comidas = await _repository.FindAsync(comida => comida.UsuarioId == usuarioId);
        return comidas.Select(comida => comida.ToResponseDto());
    }

    public async Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByFechaAsync(int usuarioId, DateTime fecha)
    {
        var fechaInicio = fecha.Date;
        var fechaFin = fechaInicio.AddDays(1);

        var comidas = await _repository.FindAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= fechaInicio &&
            comida.FechaAsignada < fechaFin);

        return comidas.Select(comida => comida.ToResponseDto());
    }

    public async Task<ComidaPlanificadaResponseDto?> GetByIdAsync(int id)
    {
        var comida = await _repository.GetByIdAsync(id);
        return comida?.ToResponseDto();
    }

    public async Task<ComidaPlanificadaResponseDto> CreateAsync(ComidaPlanificadaCreateDto comida)
    {
        if (await ExisteConflictoAsync(comida.UsuarioId, comida.FechaAsignada, comida.Turno))
        {
            throw new InvalidOperationException("Ya existe una comida planificada para ese usuario, fecha y turno.");
        }

        var entity = new ComidaPlanificada
        {
            UsuarioId = comida.UsuarioId,
            RecetaId = comida.RecetaId,
            FechaAsignada = comida.FechaAsignada,
            Turno = comida.Turno.Trim()
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, ComidaPlanificadaUpdateDto comida)
    {
        var existingComida = await _repository.GetByIdAsync(id);

        if (existingComida is null)
        {
            return false;
        }

        if (await _repository.AnyAsync(existing =>
                existing.Id != id &&
                existing.UsuarioId == comida.UsuarioId &&
                existing.FechaAsignada.Date == comida.FechaAsignada.Date &&
                existing.Turno == comida.Turno.Trim()))
        {
            throw new InvalidOperationException("Ya existe una comida planificada para ese usuario, fecha y turno.");
        }

        existingComida.UsuarioId = comida.UsuarioId;
        existingComida.RecetaId = comida.RecetaId;
        existingComida.FechaAsignada = comida.FechaAsignada;
        existingComida.Turno = comida.Turno.Trim();

        await _repository.UpdateAsync(existingComida);

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

    private async Task<bool> ExisteConflictoAsync(int usuarioId, DateTime fecha, string turno)
    {
        var fechaInicio = fecha.Date;
        var fechaFin = fechaInicio.AddDays(1);
        var turnoNormalizado = turno.Trim();

        return await _repository.AnyAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= fechaInicio &&
            comida.FechaAsignada < fechaFin &&
            comida.Turno == turnoNormalizado);
    }
}
