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
    private readonly IRepository<Pago> _repository;
    private readonly IRepository<ContratoSub> _contratoRepository;
    private readonly IRepository<Usuario> _usuarioRepository;

    public PagoService(
        IRepository<Pago> repository,
        IRepository<ContratoSub> contratoRepository,
        IRepository<Usuario> usuarioRepository)
    {
        _repository = repository;
        _contratoRepository = contratoRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IEnumerable<PagoResponseDto>> GetAllAsync(int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

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
        await ValidarPermisosAdminAsync(solicitanteId);

        var pago = await _repository.GetByIdAsync(id);
        return pago?.ToResponseDto();
    }

    public async Task<PagoResponseDto> CreateAsync(PagoCreateDto pago, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var contrato = await _contratoRepository.GetByIdAsync(pago.ContratoSubId);

        if (contrato is null)
        {
            throw new InvalidOperationException("El contrato de suscripcion no existe.");
        }

        if (pago.Monto != contrato.Monto)
        {
            throw new InvalidOperationException("El monto del pago debe coincidir con el monto del contrato.");
        }

        var entity = new Pago
        {
            UsuarioId = contrato.UsuarioId,
            ContratoSubId = pago.ContratoSubId,
            FechaPago = DateTime.UtcNow,
            Monto = pago.Monto,
            EstadoPago = EstadoPago.Pendiente,
            MetodoPago = pago.MetodoPago
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, PagoUpdateDto pago, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var existingPago = await _repository.GetByIdAsync(id);

        if (existingPago is null)
        {
            return false;
        }

        existingPago.Monto = pago.Monto;
        existingPago.EstadoPago = pago.EstadoPago;
        existingPago.MetodoPago = pago.MetodoPago;

        await _repository.UpdateAsync(existingPago);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var pago = await _repository.GetByIdAsync(id);

        if (pago is null)
        {
            return false;
        }

        await _repository.DeleteAsync(pago);

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
}
