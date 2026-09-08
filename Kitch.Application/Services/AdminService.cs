using Kitch.Application.DTOs.Admin;
using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.DTOs.Usuarios;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class AdminService : IAdminService
{
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<Suscripcion> _suscripcionRepository;
    private readonly IRepository<ContratoSub> _contratoRepository;
    private readonly IRepository<Pago> _pagoRepository;

    public AdminService(
        IRepository<Usuario> usuarioRepository,
        IRepository<Suscripcion> suscripcionRepository,
        IRepository<ContratoSub> contratoRepository,
        IRepository<Pago> pagoRepository)
    {
        _usuarioRepository = usuarioRepository;
        _suscripcionRepository = suscripcionRepository;
        _contratoRepository = contratoRepository;
        _pagoRepository = pagoRepository;
    }

    public async Task<MetricasPlataformaDto> GetMetricasAsync(int usuarioId)
    {
        await ValidarPermisosAdminAsync(usuarioId);

        var pagosAprobados = await _pagoRepository.FindAsync(pago =>
            pago.EstadoPago == EstadoPago.Aprobado);

        return new MetricasPlataformaDto
        {
            UsuariosTotales = await _usuarioRepository.CountAsync(_ => true),
            UsuariosActivos = await _usuarioRepository.CountAsync(u => u.Activo),
            UsuariosBasicos = await _usuarioRepository.CountAsync(u => u.Rol == RolUsuario.Basico),
            UsuariosProfesionales = await _usuarioRepository.CountAsync(u => u.Rol == RolUsuario.Profesional),
            UsuariosAdmin = await _usuarioRepository.CountAsync(u => u.Rol == RolUsuario.Admin),
            SuscripcionesTotales = await _suscripcionRepository.CountAsync(_ => true),
            SuscripcionesActivas = await _suscripcionRepository.CountAsync(s => s.Activa),
            ContratosActivos = await _contratoRepository.CountAsync(c => c.Estado == EstadoContratoSub.Activo),
            PagosAprobados = pagosAprobados.Count,
            IngresosTotales = pagosAprobados.Sum(pago => pago.Monto)
        };
    }

    public async Task<IEnumerable<UsuarioResponseDto>> GetUsuariosAsync(int usuarioId)
    {
        await ValidarPermisosAdminAsync(usuarioId);

        var usuarios = await _usuarioRepository.GetAllAsync();
        return usuarios.Select(usuario => usuario.ToResponseDto());
    }

    public async Task<IEnumerable<SuscripcionResponseDto>> GetSuscripcionesAsync(int usuarioId)
    {
        await ValidarPermisosAdminAsync(usuarioId);

        var suscripciones = await _suscripcionRepository.GetAllAsync();
        return suscripciones.Select(suscripcion => suscripcion.ToResponseDto());
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
