using Kitch.Application.DTOs.Suscripciones;
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

    public async Task<ContratarSuscripcionResult> ContratarAsync(int usuarioId, ContratarSuscripcionRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Los datos de la contratación son obligatorios.", nameof(request));
        }

        if (request.Monto <= 0)
        {
            throw new ArgumentException("El monto debe ser mayor a cero.", nameof(request));
        }

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

        var ahora = DateTime.UtcNow;

        // Cobro agnóstico al proveedor (Stripe, MercadoPago, etc. se resuelven en Infrastructure).
        var cobro = await _paymentGateway.ProcesarPagoAsync(new PaymentGatewayRequest
        {
            UsuarioId = usuarioId,
            Monto = request.Monto,
            MetodoPago = request.MetodoPago,
            EmailUsuario = usuario.Email,
            Descripcion = $"Suscripción {request.Tipo} - Alacena Virtual"
        });

        var aprobado = cobro.Aprobado;

        var suscripcion = await _repository.AddAsync(new Suscripcion
        {
            UsuarioId = usuarioId,
            Tipo = request.Tipo,
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(1),
            Activa = aprobado
        });

        var contrato = await _contratoRepository.AddAsync(new ContratoSub
        {
            UsuarioId = usuarioId,
            SuscripcionId = suscripcion.Id,
            FechaContratacion = ahora,
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(1),
            Monto = request.Monto,
            Estado = aprobado ? EstadoContratoSub.Activo : EstadoContratoSub.Cancelado
        });

        var pago = await _pagoRepository.AddAsync(new Pago
        {
            UsuarioId = usuarioId,
            ContratoSubId = contrato.Id,
            FechaPago = ahora,
            Monto = request.Monto,
            MetodoPago = request.MetodoPago,
            EstadoPago = aprobado ? EstadoPago.Aprobado : EstadoPago.Rechazado
        });

        if (aprobado)
        {
            usuario.Rol = RolUsuario.Profesional;
            await _usuarioRepository.UpdateAsync(usuario);
        }

        return new ContratarSuscripcionResult
        {
            Aprobado = aprobado,
            Mensaje = aprobado
                ? "Pago aprobado. ¡Bienvenido al nivel Profesional!"
                : (string.IsNullOrWhiteSpace(cobro.Mensaje)
                    ? "No se pudo procesar el pago. Verificá los datos de tu tarjeta o intentá con otro medio de pago."
                    : cobro.Mensaje),
            RolUsuario = usuario.Rol,
            ContratoId = contrato.Id,
            PagoId = pago.Id,
            EstadoPago = pago.EstadoPago
        };
    }

    public Task<CheckoutSuscripcionResponseDto> IniciarCheckoutAsync(
        int usuarioId,
        CheckoutSuscripcionRequestDto request)
    {
        if (request is null || request.Monto <= 0)
        {
            throw new ArgumentException("El monto debe ser mayor a cero.");
        }

        // Cascarón Frontend-Ready: el front abre esta URL / PreferenceId en su flujo de pago.
        // Luego se reemplaza con Stripe Checkout Session o Preference de MercadoPago reales.
        var preferenceId = $"pref_{usuarioId}_{Guid.NewGuid():N}"[..32];
        var pasarela = string.IsNullOrWhiteSpace(request.Pasarela) ? "MercadoPago" : request.Pasarela.Trim();

        return Task.FromResult(new CheckoutSuscripcionResponseDto
        {
            PreferenceId = preferenceId,
            CheckoutUrl = $"https://checkout.simulated.local/{pasarela.ToLowerInvariant()}?preference_id={preferenceId}&monto={request.Monto}",
            Mensaje = "Checkout simulado listo. El front puede abrir CheckoutUrl o PreferenceId en su modal de pago."
        });
    }

    public async Task<WebhookPagoResponseDto> ProcesarWebhookAsync(WebhookPagoRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PreferenceId))
        {
            throw new ArgumentException("PreferenceId es obligatorio.");
        }

        if (request.UsuarioId <= 0)
        {
            throw new ArgumentException("UsuarioId es obligatorio.");
        }

        var estado = request.Estado?.Trim().ToLowerInvariant() ?? string.Empty;
        if (estado is not ("approved" or "aprobado"))
        {
            return new WebhookPagoResponseDto
            {
                Procesado = false,
                Mensaje = "Pago no aprobado. El rol del usuario no se modificó.",
                RolUsuario = null
            };
        }

        var usuario = await _usuarioRepository.GetByIdAsync(request.UsuarioId)
            ?? throw new InvalidOperationException("El usuario no existe.");

        if (usuario.Rol == RolUsuario.Profesional)
        {
            return new WebhookPagoResponseDto
            {
                Procesado = true,
                Mensaje = "El usuario ya era Profesional.",
                RolUsuario = usuario.Rol
            };
        }

        var ahora = DateTime.UtcNow;
        var monto = request.Monto > 0 ? request.Monto : 9.99m;

        var suscripcion = await _repository.AddAsync(new Suscripcion
        {
            UsuarioId = usuario.Id,
            Tipo = "Profesional",
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(1),
            Activa = true
        });

        await _contratoRepository.AddAsync(new ContratoSub
        {
            UsuarioId = usuario.Id,
            SuscripcionId = suscripcion.Id,
            FechaContratacion = ahora,
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(1),
            Monto = monto,
            Estado = EstadoContratoSub.Activo
        });

        usuario.Rol = RolUsuario.Profesional;
        await _usuarioRepository.UpdateAsync(usuario);

        return new WebhookPagoResponseDto
        {
            Procesado = true,
            Mensaje = "Webhook simulado OK. Usuario ascendido a Profesional.",
            RolUsuario = usuario.Rol
        };
    }
}
