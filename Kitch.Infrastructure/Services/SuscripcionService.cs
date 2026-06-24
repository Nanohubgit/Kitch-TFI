using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class SuscripcionService : ISuscripcionService
{
    private readonly IRepository<Suscripcion> _repository;

    public SuscripcionService(IRepository<Suscripcion> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SuscripcionResponseDto>> GetAllAsync()
    {
        var suscripciones = await _repository.GetAllAsync();
        return suscripciones.Select(suscripcion => suscripcion.ToResponseDto());
    }

    public async Task<SuscripcionResponseDto?> GetByIdAsync(int id)
    {
        var suscripcion = await _repository.GetByIdAsync(id);
        return suscripcion?.ToResponseDto();
    }

    public async Task<SuscripcionResponseDto> CreateAsync(SuscripcionCreateDto suscripcion)
    {
        if (suscripcion.Activa && await _repository.AnyAsync(existing =>
                existing.UsuarioId == suscripcion.UsuarioId && existing.Activa))
        {
            throw new InvalidOperationException("El usuario ya tiene una suscripcion activa.");
        }

        ValidateFechas(suscripcion.FechaInicio, suscripcion.FechaFin);

        var entity = new Suscripcion
        {
            UsuarioId = suscripcion.UsuarioId,
            FechaInicio = suscripcion.FechaInicio,
            FechaFin = suscripcion.FechaFin,
            Activa = suscripcion.Activa,
            Tipo = suscripcion.Tipo.Trim()
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, SuscripcionUpdateDto suscripcion)
    {
        var existingSuscripcion = await _repository.GetByIdAsync(id);

        if (existingSuscripcion is null)
        {
            return false;
        }

        if (suscripcion.Activa && await _repository.AnyAsync(existing =>
                existing.Id != id &&
                existing.UsuarioId == suscripcion.UsuarioId &&
                existing.Activa))
        {
            throw new InvalidOperationException("El usuario ya tiene una suscripcion activa.");
        }

        ValidateFechas(suscripcion.FechaInicio, suscripcion.FechaFin);

        existingSuscripcion.UsuarioId = suscripcion.UsuarioId;
        existingSuscripcion.FechaInicio = suscripcion.FechaInicio;
        existingSuscripcion.FechaFin = suscripcion.FechaFin;
        existingSuscripcion.Activa = suscripcion.Activa;
        existingSuscripcion.Tipo = suscripcion.Tipo.Trim();

        await _repository.UpdateAsync(existingSuscripcion);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var suscripcion = await _repository.GetByIdAsync(id);

        if (suscripcion is null)
        {
            return false;
        }

        await _repository.DeleteAsync(suscripcion);

        return true;
    }

    private static void ValidateFechas(DateTime fechaInicio, DateTime? fechaFin)
    {
        if (fechaFin.HasValue && fechaFin.Value <= fechaInicio)
        {
            throw new InvalidOperationException("La fecha de fin debe ser posterior a la fecha de inicio.");
        }
    }
}
