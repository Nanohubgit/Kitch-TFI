using Kitch.Application.DTOs.ContratosSub;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ContratoSubService : IContratoSubService
{
    private readonly IRepository<ContratoSub> _repository;
    private readonly IRepository<Usuario> _usuarioRepository;

    public ContratoSubService(
        IRepository<ContratoSub> repository,
        IRepository<Usuario> usuarioRepository)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IEnumerable<ContratoSubResponseDto>> GetAllAsync(int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var contratos = await _repository.GetAllAsync();
        return contratos.Select(contrato => contrato.ToResponseDto());
    }

    public async Task<IEnumerable<ContratoSubResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var contratos = await _repository.FindAsync(contratoSub => contratoSub.UsuarioId == usuarioId);
        return contratos.Select(contrato => contrato.ToResponseDto());
    }

    public async Task<ContratoSubResponseDto?> GetByIdAsync(int id, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var contrato = await _repository.GetByIdAsync(id);
        return contrato?.ToResponseDto();
    }

    public async Task<ContratoSubResponseDto> CreateAsync(ContratoSubCreateDto contratoSub, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

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

    public async Task<bool> UpdateAsync(int id, ContratoSubUpdateDto contratoSub, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

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

    public async Task<bool> DeleteAsync(int id, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var contratoSub = await _repository.GetByIdAsync(id);

        if (contratoSub is null)
        {
            return false;
        }

        await _repository.DeleteAsync(contratoSub);

        return true;
    }

    private async Task ValidarPermisosAdminAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is null)
        {
            throw new UnauthorizedAccessException();
        }

        if (usuario.Rol != RolUsuario.Admin)
        {
            throw new ForbiddenException(
                "Acceso denegado. Se requieren permisos de administrador para visualizar esta información.");
        }
    }

    private static void ValidateFechas(DateTime fechaInicio, DateTime fechaFin)
    {
        if (fechaFin <= fechaInicio)
        {
            throw new InvalidOperationException("La fecha de fin debe ser posterior a la fecha de inicio.");
        }
    }
}
