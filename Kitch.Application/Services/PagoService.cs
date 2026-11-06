using Kitch.Application.DTOs.Pagos;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class PagoService : IPagoService
{
    private const string MensajeAdminConsulta =
        "Acceso denegado. Se requieren permisos de administrador para visualizar esta información.";

    private const string MensajeHistorialSoloLectura =
        "El historial de pagos, contratos y suscripciones es de solo lectura. No se puede modificar ni eliminar.";

    private readonly IRepository<Pago> _repository;
    private readonly IRepository<Usuario> _usuarioRepository;

    public PagoService(
        IRepository<Pago> repository,
        IRepository<Usuario> usuarioRepository)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IEnumerable<PagoResponseDto>> GetAllAsync(int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId, MensajeAdminConsulta);

        var pagos = await _repository.GetAllAsync();
        return pagos.Select(pago => pago.ToResponseDto());
    }

    public async Task<IEnumerable<PagoResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var pagos = await _repository.FindAsync(pago => pago.UsuarioId == usuarioId);
        return pagos.Select(pago => pago.ToResponseDto());
    }

    public async Task<PagoResponseDto?> GetByIdAsync(int id, int solicitanteId)
    {
        var pago = await _repository.GetByIdAsync(id);
        if (pago is null)
        {
            return null;
        }

        if (pago.UsuarioId != solicitanteId)
        {
            await ValidarPermisosAdminAsync(solicitanteId, MensajeAdminConsulta);
        }

        return pago.ToResponseDto();
    }

    public Task<PagoResponseDto> CreateAsync(PagoCreateDto pago, int solicitanteId) =>
        throw new ForbiddenException(MensajeHistorialSoloLectura);

    public Task<bool> UpdateAsync(int id, PagoUpdateDto pago, int solicitanteId) =>
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
