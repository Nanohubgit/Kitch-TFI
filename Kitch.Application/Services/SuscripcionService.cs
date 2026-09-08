using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

/// <summary>
/// Orquesta el upgrade Básico → Profesional: valida estado, cobra vía pasarela
/// y, si el pago es exitoso, registra contrato/pago y actualiza el rol.
/// </summary>
public class SuscripcionService : ISuscripcionService
{
    private const string MensajeAdminConsulta =
        "Acceso denegado. Se requieren permisos de administrador para visualizar esta información.";

    private const string MensajeHistorialSoloLectura =
        "El historial de pagos, contratos y suscripciones es de solo lectura. No se puede modificar ni eliminar.";

    private readonly IRepository<Suscripcion> _repository;
    private readonly IRepository<ContratoSub> _contratoRepository;
    private readonly IRepository<Pago> _pagoRepository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IPaymentGatewayService _paymentGateway;

    public SuscripcionService(
        IRepository<Suscripcion> repository,
        IRepository<ContratoSub> contratoRepository,
        IRepository<Pago> pagoRepository,
        IRepository<Usuario> usuarioRepository,
        IPaymentGatewayService paymentGateway)
    {
        _repository = repository;
        _contratoRepository = contratoRepository;
        _pagoRepository = pagoRepository;
        _usuarioRepository = usuarioRepository;
        _paymentGateway = paymentGateway;
    }

    public async Task<IEnumerable<SuscripcionResponseDto>> GetAllAsync(int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId, MensajeAdminConsulta);

        var suscripciones = await _repository.GetAllAsync();
        return suscripciones.Select(suscripcion => suscripcion.ToResponseDto());
    }

    public async Task<IEnumerable<SuscripcionResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var suscripciones = await _repository.FindAsync(suscripcion => suscripcion.UsuarioId == usuarioId);
        return suscripciones.Select(suscripcion => suscripcion.ToResponseDto());
    }

    public async Task<SuscripcionResponseDto?> GetByIdAsync(int id, int solicitanteId)
    {
        var suscripcion = await _repository.GetByIdAsync(id);
        if (suscripcion is null)
        {
            return null;
        }

        if (suscripcion.UsuarioId != solicitanteId)
        {
            await ValidarPermisosAdminAsync(solicitanteId, MensajeAdminConsulta);
        }

        return suscripcion.ToResponseDto();
    }

    public async Task<SuscripcionResponseDto> CreateAsync(SuscripcionCreateDto suscripcion, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId, MensajeHistorialSoloLectura);

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

    public async Task<bool> UpdateAsync(int id, SuscripcionUpdateDto suscripcion, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId, MensajeHistorialSoloLectura);

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

    public async Task<bool> DeleteAsync(int id, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId, MensajeHistorialSoloLectura);

        var suscripcion = await _repository.GetByIdAsync(id);

        if (suscripcion is null)
        {
            return false;
        }

        await _repository.DeleteAsync(suscripcion);

        return true;
    }

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

    private static void ValidateFechas(DateTime fechaInicio, DateTime? fechaFin)
    {
        if (fechaFin.HasValue && fechaFin.Value <= fechaInicio)
        {
            throw new InvalidOperationException("La fecha de fin debe ser posterior a la fecha de inicio.");
        }
    }

    public async Task<IniciarPagoResponseDto> ContratarAsync(int usuarioId, ContratarSuscripcionRequest? request)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("El usuario no existe.");

        if (usuario.Rol == RolUsuario.Profesional)
        {
            throw new InvalidOperationException("El usuario ya posee el rol Profesional.");
        }

        var tieneContratoActivo = await _contratoRepository.AnyAsync(contrato =>
            contrato.UsuarioId == usuarioId && contrato.Estado == EstadoContratoSub.Activo);

        if (tieneContratoActivo)
        {
            throw new InvalidOperationException("El usuario ya tiene una suscripción activa.");
        }

        var tipo = string.IsNullOrWhiteSpace(request?.Tipo)
            ? PrecioSuscripcion.TipoProfesional
            : request.Tipo.Trim();

        var checkout = await _paymentGateway.CrearPreferenciaAsync(new PaymentGatewayRequest
        {
            UsuarioId = usuarioId,
            Monto = PrecioSuscripcion.ProfesionalArs,
            MetodoPago = request?.MetodoPago ?? MetodoPago.TarjetaCredito,
            EmailUsuario = usuario.Email,
            Descripcion = $"Suscripción {tipo} - Alacena Virtual"
        });

        return new IniciarPagoResponseDto
        {
            InitPoint = checkout.InitPoint,
            PreferenceId = checkout.PreferenceId,
            Monto = PrecioSuscripcion.ProfesionalArs,
            Moneda = PrecioSuscripcion.MonedaArs,
            Mensaje = "Redirigí al usuario a InitPoint para completar el pago. El rol se actualiza solo cuando Mercado Pago confirma por webhook."
        };
    }

    public async Task ProcesarNotificacionPagoAsync(string paymentId)
    {
        var cobro = await _paymentGateway.ConsultarPagoAsync(paymentId);
        if (cobro is null || !cobro.Aprobado)
        {
            return;
        }

        if (!int.TryParse(cobro.ExternalReference, out var usuarioId))
        {
            throw new InvalidOperationException("La notificación no incluye un UsuarioId válido en ExternalReference.");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException($"No existe el usuario {usuarioId} indicado por la pasarela.");

        if (usuario.Rol == RolUsuario.Profesional &&
            await _contratoRepository.AnyAsync(contrato =>
                contrato.UsuarioId == usuarioId && contrato.Estado == EstadoContratoSub.Activo))
        {
            return;
        }

        var ahora = DateTime.UtcNow;
        var monto = cobro.Monto > 0 ? cobro.Monto : PrecioSuscripcion.ProfesionalArs;

        var suscripcion = await _repository.AddAsync(new Suscripcion
        {
            UsuarioId = usuarioId,
            Tipo = PrecioSuscripcion.TipoProfesional,
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(1),
            Activa = true
        });

        var contrato = await _contratoRepository.AddAsync(new ContratoSub
        {
            UsuarioId = usuarioId,
            SuscripcionId = suscripcion.Id,
            FechaContratacion = ahora,
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(1),
            Monto = monto,
            Estado = EstadoContratoSub.Activo
        });

        await _pagoRepository.AddAsync(new Pago
        {
            UsuarioId = usuarioId,
            ContratoSubId = contrato.Id,
            FechaPago = ahora,
            Monto = monto,
            MetodoPago = MetodoPago.TarjetaCredito,
            EstadoPago = EstadoPago.Aprobado
        });

        usuario.Rol = RolUsuario.Profesional;
        await _usuarioRepository.UpdateAsync(usuario);
    }
}
