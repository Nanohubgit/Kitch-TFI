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
    private const string MensajeAdminConsulta =
        "Acceso denegado. Se requieren permisos de administrador para visualizar esta información.";

    private const string MensajeHistorialSoloLectura =
        "El historial de pagos, contratos y suscripciones es de solo lectura. No se puede modificar ni eliminar.";

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
        await ValidarPermisosAdminAsync(solicitanteId, MensajeAdminConsulta);

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
        var contrato = await _repository.GetByIdAsync(id);
        if (contrato is null)
        {
            return null;
        }

        if (contrato.UsuarioId != solicitanteId)
        {
            await ValidarPermisosAdminAsync(solicitanteId, MensajeAdminConsulta);
        }

        return contrato.ToResponseDto();
    }

    public Task<ContratoSubResponseDto> CreateAsync(ContratoSubCreateDto contratoSub, int solicitanteId) =>
        throw new ForbiddenException(MensajeHistorialSoloLectura);

    public Task<bool> UpdateAsync(int id, ContratoSubUpdateDto contratoSub, int solicitanteId) =>
        throw new ForbiddenException(MensajeHistorialSoloLectura);

    public Task<bool> DeleteAsync(int id, int solicitanteId) =>
        throw new ForbiddenException(MensajeHistorialSoloLectura);

    private async Task ValidarPermisosAdminAsync(int usuarioId, string mensaje)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is null)
        {
            throw new UnauthorizedAccessException();
        }

        if (usuario.Rol != RolUsuario.Admin)
        {
            throw new ForbiddenException(mensaje);
        }
    }
}
