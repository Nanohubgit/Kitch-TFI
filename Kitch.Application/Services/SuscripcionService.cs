using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class SuscripcionService : ISuscripcionService
{
    private const string RolProfesional = "Profesional";

    private readonly IRepository<Suscripcion> _repository;
    private readonly IRepository<ContratoSub> _contratoRepository;
    private readonly IRepository<Pago> _pagoRepository;
    private readonly IRepository<Usuario> _usuarioRepository;

    public SuscripcionService(
        IRepository<Suscripcion> repository,
        IRepository<ContratoSub> contratoRepository,
        IRepository<Pago> pagoRepository,
        IRepository<Usuario> usuarioRepository)
    {
        _repository = repository;
        _contratoRepository = contratoRepository;
        _pagoRepository = pagoRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IEnumerable<Suscripcion>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Suscripcion?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Suscripcion> CreateAsync(Suscripcion suscripcion)
    {
        return await _repository.AddAsync(suscripcion);
    }

    public async Task<bool> UpdateAsync(int id, Suscripcion suscripcion)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        suscripcion.Id = id;
        await _repository.UpdateAsync(suscripcion);

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

        // Regla de negocio: un usuario solo puede tener una suscripción activa al mismo tiempo.
        var tieneContratoActivo = await _contratoRepository.AnyAsync(contrato =>
            contrato.UsuarioId == usuarioId && contrato.Estado == EstadoContratoSub.Activo);

        if (tieneContratoActivo)
        {
            throw new InvalidOperationException("El usuario ya tiene una suscripción activa.");
        }

        var ahora = DateTime.UtcNow;
        var aprobado = !request.SimularRechazo;

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
            usuario.Rol = RolProfesional;
            await _usuarioRepository.UpdateAsync(usuario);
        }

        return new ContratarSuscripcionResult
        {
            Aprobado = aprobado,
            Mensaje = aprobado
                ? "Pago aprobado. ¡Bienvenido al nivel Profesional!"
                : "No se pudo procesar el pago. Verificá los datos de tu tarjeta o intentá con otro medio de pago.",
            RolUsuario = usuario.Rol,
            ContratoId = contrato.Id,
            PagoId = pago.Id,
            EstadoPago = pago.EstadoPago
        };
    }
}
