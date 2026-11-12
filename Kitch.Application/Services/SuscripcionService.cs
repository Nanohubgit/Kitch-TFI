using Kitch.Application.Common;
using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class SuscripcionService : ISuscripcionService
{
    private const string MensajeAdminConsulta =
        "Acceso denegado. Se requieren permisos de administrador para visualizar esta información.";

    private const string MensajeHistorialSoloLectura =
        "El historial de pagos, contratos y suscripciones es de solo lectura. No se puede modificar ni eliminar.";

    private const int MaxTransaccionId = 50;

    private readonly IRepository<Suscripcion> _repository;
    private readonly IRepository<ContratoSub> _contratoRepository;
    private readonly IRepository<Pago> _pagoRepository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly PlanSuscripcionConfig _plan;

    public SuscripcionService(
        IRepository<Suscripcion> repository,
        IRepository<ContratoSub> contratoRepository,
        IRepository<Pago> pagoRepository,
        IRepository<Usuario> usuarioRepository,
        IPaymentGatewayService paymentGateway,
        IUnidadDeTrabajo unidadDeTrabajo,
        PlanSuscripcionConfig plan)
    {
        _plan = plan;
        _repository = repository;
        _contratoRepository = contratoRepository;
        _pagoRepository = pagoRepository;
        _usuarioRepository = usuarioRepository;
        _paymentGateway = paymentGateway;
        _unidadDeTrabajo = unidadDeTrabajo;
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

    public Task<SuscripcionResponseDto> CreateAsync(SuscripcionCreateDto suscripcion, int solicitanteId) =>
        throw new ForbiddenException(MensajeHistorialSoloLectura);

    public Task<bool> UpdateAsync(int id, SuscripcionUpdateDto suscripcion, int solicitanteId) =>
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

    public async Task<IniciarPagoResponseDto> ContratarAsync(int usuarioId, ContratarSuscripcionRequest? request)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("El usuario no existe.");

        if (usuario.Rol == RolUsuario.Admin)
        {
            throw new InvalidOperationException("Las cuentas administradoras no necesitan suscripción.");
        }

        var esRenovacion = usuario.Rol == RolUsuario.Profesional;

        var tipo = _plan.TipoProfesional;

        var checkout = await _paymentGateway.CrearPreferenciaAsync(new PaymentGatewayRequest
        {
            UsuarioId = usuarioId,
            Monto = _plan.PrecioProfesionalArs,
            Moneda = _plan.Moneda,
            MetodoPago = request?.MetodoPago ?? MetodoPago.TarjetaCredito,
            EmailUsuario = usuario.Email,
            Descripcion = esRenovacion
                ? $"Renovación suscripción {tipo} - Alacena Virtual"
                : $"Suscripción {tipo} - Alacena Virtual"
        });

        return new IniciarPagoResponseDto
        {
            InitPoint = checkout.InitPoint,
            PreferenceId = checkout.PreferenceId,
            Monto = _plan.PrecioProfesionalArs,
            Moneda = _plan.Moneda,
            Mensaje = esRenovacion
                ? "Te llevamos a Mercado Pago para renovar. Cuando se apruebe el pago sumamos un mes a tu vencimiento actual."
                : "Te llevamos a Mercado Pago para completar el pago. Tu plan se actualiza apenas se apruebe."
        };
    }

    public PlanSuscripcionResponseDto GetPlan() => new()
    {
        Tipo = _plan.TipoProfesional,
        Monto = _plan.PrecioProfesionalArs,
        Moneda = _plan.Moneda
    };

    public async Task<EstadoPagoResponseDto> ConfirmarPagoAsync(int usuarioId, string paymentId)
    {
        var id = paymentId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Indicá el id del pago de Mercado Pago.");
        }

        var cobro = await _paymentGateway.ConsultarPagoAsync(id)
            ?? throw new KeyNotFoundException("No encontramos ese pago en Mercado Pago.");

        if (!int.TryParse(cobro.ExternalReference, out var duenioId) || duenioId != usuarioId)
        {
            throw new ForbiddenException("Ese pago no corresponde a tu cuenta.");
        }

        var status = cobro.Status.Trim().ToLowerInvariant();
        var rechazado = status is "rejected" or "cancelled" or "refunded" or "charged_back";
        var pendiente = status is "pending" or "in_process" or "authorized" or "in_mediation";

        if (cobro.Aprobado)
        {
            await AplicarCobroAsync(cobro, id);
        }

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new UnauthorizedAccessException();
        var transaccionId = ObtenerTransaccionId(cobro, id);
        var pagoRegistrado = cobro.Aprobado &&
            await _pagoRepository.AnyAsync(pago => pago.TransaccionId == transaccionId && pago.UsuarioId == usuarioId);
        var esProfesional = usuario.Rol == RolUsuario.Profesional;
        var profesionalHasta = esProfesional ? await ObtenerFinVigenteAsync(usuarioId) : null;

        var mensaje = cobro.Aprobado
            ? esProfesional && pagoRegistrado
                ? $"Pago aprobado. Sos Profesional hasta {FormatearFecha(profesionalHasta)}."
                : pagoRegistrado
                    ? "Este pago ya se usó para una suscripción anterior que ya venció. Para volver a Profesional, hacé un pago nuevo."
                    : usuario.Rol == RolUsuario.Admin
                        ? "Las cuentas administradoras no necesitan suscripción."
                        : !usuario.Activo
                            ? "Tu cuenta está bloqueada. Contactá a un administrador."
                            : "El pago figura aprobado pero el monto no coincide con el plan Profesional. Contactá a soporte."
            : rechazado
                ? "El pago fue rechazado o cancelado. Tu plan no cambió; podés intentarlo de nuevo con otro medio de pago."
                : pendiente
                    ? "El pago todavía está pendiente de acreditación. Cuando Mercado Pago lo apruebe, se actualiza tu plan."
                    : $"Estado del pago: {cobro.Status}.";

        return new EstadoPagoResponseDto
        {
            PaymentId = id,
            Status = cobro.Status,
            Aprobado = cobro.Aprobado,
            Rechazado = rechazado,
            Pendiente = pendiente,
            RolActual = usuario.Rol,
            RequiereRefrescarSesion = cobro.Aprobado && esProfesional && pagoRegistrado,
            ProfesionalHasta = profesionalHasta,
            Mensaje = mensaje
        };
    }

    public async Task ProcesarNotificacionPagoAsync(string paymentId)
    {
        var cobro = await _paymentGateway.ConsultarPagoAsync(paymentId);
        if (cobro is null || !cobro.Aprobado)
        {
            return;
        }

        await AplicarCobroAsync(cobro, paymentId);
    }

    private async Task AplicarCobroAsync(PaymentNotificationResult cobro, string paymentId)
    {
        if (!int.TryParse(cobro.ExternalReference, out var usuarioId))
        {
            throw new InvalidOperationException("La notificación no incluye un UsuarioId válido en ExternalReference.");
        }

        var transaccionId = ObtenerTransaccionId(cobro, paymentId);

        try
        {
            await _unidadDeTrabajo.EjecutarAsync(async () =>
            {
                await _unidadDeTrabajo.BloquearRecursoAsync($"kitch:pagos:usuario:{usuarioId}");
                await AplicarPagoAprobadoAsync(cobro, usuarioId, transaccionId);
            });
        }
        catch (ConflictException)
        {
            if (!await _pagoRepository.AnyAsync(pago => pago.TransaccionId == transaccionId))
            {
                throw;
            }
        }
    }

    private async Task AplicarPagoAprobadoAsync(PaymentNotificationResult cobro, int usuarioId, string transaccionId)
    {
        if (await _pagoRepository.AnyAsync(pago => pago.TransaccionId == transaccionId))
        {
            return;
        }

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException($"No existe el usuario {usuarioId} indicado por la pasarela.");

        if (usuario.Rol == RolUsuario.Admin || !usuario.Activo)
        {
            return;
        }

        var monto = cobro.Monto > 0 ? cobro.Monto : 0m;
        if (Math.Abs(monto - _plan.PrecioProfesionalArs) > 1m)
        {
            return;
        }

        var ahora = DateTime.UtcNow;
        var contrato = usuario.Rol == RolUsuario.Profesional
            ? await ExtenderSuscripcionVigenteAsync(usuarioId, monto, ahora)
            : null;

        contrato ??= await CrearSuscripcionNuevaAsync(usuarioId, monto, ahora);

        await _pagoRepository.AddAsync(new Pago
        {
            UsuarioId = usuarioId,
            ContratoSubId = contrato.Id,
            FechaPago = ahora,
            Monto = monto,
            MetodoPago = cobro.MetodoPago,
            EstadoPago = EstadoPago.Aprobado,
            TransaccionId = transaccionId
        });

        if (usuario.Rol != RolUsuario.Profesional)
        {
            usuario.Rol = RolUsuario.Profesional;
            await _usuarioRepository.UpdateAsync(usuario);
        }
    }

    private async Task<ContratoSub?> ExtenderSuscripcionVigenteAsync(int usuarioId, decimal monto, DateTime ahora)
    {
        var suscripcion = (await _repository.FindAsync(item =>
                item.UsuarioId == usuarioId && item.Activa && (item.FechaFin == null || item.FechaFin > ahora)))
            .OrderByDescending(item => item.FechaFin ?? DateTime.MaxValue)
            .FirstOrDefault();

        if (suscripcion is null)
        {
            return null;
        }

        var contrato = (await _contratoRepository.FindAsync(item =>
                item.SuscripcionId == suscripcion.Id && item.Estado == EstadoContratoSub.Activo))
            .FirstOrDefault();

        if (contrato is null)
        {
            return null;
        }

        var nuevoFin = (suscripcion.FechaFin ?? ahora).AddMonths(1);
        suscripcion.FechaFin = nuevoFin;
        await _repository.UpdateAsync(suscripcion);

        contrato.FechaFin = nuevoFin;
        contrato.Monto += monto;
        await _contratoRepository.UpdateAsync(contrato);

        return contrato;
    }

    private async Task<ContratoSub> CrearSuscripcionNuevaAsync(int usuarioId, decimal monto, DateTime ahora)
    {
        var anteriores = await _repository.FindAsync(suscripcion =>
            suscripcion.UsuarioId == usuarioId && suscripcion.Activa);
        foreach (var anterior in anteriores)
        {
            anterior.Activa = false;
            await _repository.UpdateAsync(anterior);
        }

        var contratosActivos = await _contratoRepository.FindAsync(contrato =>
            contrato.UsuarioId == usuarioId && contrato.Estado == EstadoContratoSub.Activo);
        foreach (var contratoActivo in contratosActivos)
        {
            contratoActivo.Estado = EstadoContratoSub.Vencido;
            await _contratoRepository.UpdateAsync(contratoActivo);
        }

        var suscripcion = await _repository.AddAsync(new Suscripcion
        {
            UsuarioId = usuarioId,
            Tipo = _plan.TipoProfesional,
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(1),
            Activa = true
        });

        return await _contratoRepository.AddAsync(new ContratoSub
        {
            UsuarioId = usuarioId,
            SuscripcionId = suscripcion.Id,
            FechaContratacion = ahora,
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(1),
            Monto = monto,
            Estado = EstadoContratoSub.Activo
        });
    }

    private async Task<DateTime?> ObtenerFinVigenteAsync(int usuarioId)
    {
        var ahora = DateTime.UtcNow;
        var vigentes = await _repository.FindAsync(suscripcion =>
            suscripcion.UsuarioId == usuarioId && suscripcion.Activa &&
            (suscripcion.FechaFin == null || suscripcion.FechaFin > ahora));
        return vigentes.Select(suscripcion => suscripcion.FechaFin).Max();
    }

    private static string FormatearFecha(DateTime? fechaUtc)
    {
        if (fechaUtc is null)
        {
            return "nuevo aviso";
        }

        return "el " + FechaLocal.DesdeUtc(fechaUtc.Value).ToString("dd/MM/yyyy");
    }

    private static string ObtenerTransaccionId(PaymentNotificationResult cobro, string paymentId)
    {
        var transaccionId = string.IsNullOrWhiteSpace(cobro.TransaccionId)
            ? paymentId.Trim()
            : cobro.TransaccionId.Trim();
        return transaccionId.Length > MaxTransaccionId ? transaccionId[..MaxTransaccionId] : transaccionId;
    }
}
