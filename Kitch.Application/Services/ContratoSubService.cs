using Kitch.Application.DTOs.ContratosSub;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ContratoSubService : IContratoSubService
{
    private readonly IRepository<ContratoSub> _repository;

    public ContratoSubService(IRepository<ContratoSub> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ContratoSubResponseDto>> GetAllAsync()
    {
        var contratos = await _repository.GetAllAsync();
        return contratos.Select(contrato => contrato.ToResponseDto());
    }

    public async Task<IEnumerable<ContratoSubResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var contratos = await _repository.FindAsync(contratoSub => contratoSub.UsuarioId == usuarioId);
        return contratos.Select(contrato => contrato.ToResponseDto());
    }

    public async Task<ContratoSubResponseDto?> GetByIdAsync(int id)
    {
        var contrato = await _repository.GetByIdAsync(id);
        return contrato?.ToResponseDto();
    }

    public async Task<ContratoSubResponseDto> CreateAsync(ContratoSubCreateDto contratoSub)
    {
        ValidateFechas(contratoSub.FechaInicio, contratoSub.FechaFin);

        if (await _repository.AnyAsync(existing =>
                existing.UsuarioId == contratoSub.UsuarioId && existing.Estado == EstadoContratoSub.Activo))
        {
            throw new InvalidOperationException("El usuario ya tiene un contrato activo.");
        }

        var entity = new ContratoSub
        {
            UsuarioId = contratoSub.UsuarioId,
            SuscripcionId = contratoSub.SuscripcionId,
            FechaContratacion = DateTime.UtcNow,
            FechaInicio = contratoSub.FechaInicio,
            FechaFin = contratoSub.FechaFin,
            Monto = contratoSub.Monto,
            Estado = EstadoContratoSub.Pendiente
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, ContratoSubUpdateDto contratoSub)
    {
        var existingContratoSub = await _repository.GetByIdAsync(id);

        if (existingContratoSub is null)
        {
            return false;
        }

        ValidateFechas(contratoSub.FechaInicio, contratoSub.FechaFin);

        existingContratoSub.FechaInicio = contratoSub.FechaInicio;
        existingContratoSub.FechaFin = contratoSub.FechaFin;
        existingContratoSub.Monto = contratoSub.Monto;
        existingContratoSub.Estado = contratoSub.Estado;

        await _repository.UpdateAsync(existingContratoSub);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var contratoSub = await _repository.GetByIdAsync(id);

        if (contratoSub is null)
        {
            return false;
        }

        await _repository.DeleteAsync(contratoSub);

        return true;
    }

    private static void ValidateFechas(DateTime fechaInicio, DateTime fechaFin)
    {
        if (fechaFin <= fechaInicio)
        {
            throw new InvalidOperationException("La fecha de fin debe ser posterior a la fecha de inicio.");
        }
    }
}
