using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class SuscripcionVigenciaService : ISuscripcionVigenciaService
{
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<Suscripcion> _suscripcionRepository;
    private readonly IRepository<ContratoSub> _contratoRepository;

    public SuscripcionVigenciaService(
        IRepository<Usuario> usuarioRepository,
        IRepository<Suscripcion> suscripcionRepository,
        IRepository<ContratoSub> contratoRepository)
    {
        _usuarioRepository = usuarioRepository;
        _suscripcionRepository = suscripcionRepository;
        _contratoRepository = contratoRepository;
    }

    public async Task AsegurarRolVigenteAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is null)
        {
            return;
        }

        await AsegurarRolVigenteAsync(usuario);
    }

    public async Task AsegurarRolVigenteAsync(Usuario usuario)
    {
        if (usuario.Rol != RolUsuario.Profesional)
        {
            return;
        }

        var ahora = DateTime.UtcNow;
        var vigente = await _suscripcionRepository.AnyAsync(suscripcion =>
            suscripcion.UsuarioId == usuario.Id &&
            suscripcion.Activa &&
            (suscripcion.FechaFin == null || suscripcion.FechaFin > ahora));

        if (vigente)
        {
            return;
        }

        var vencidas = await _suscripcionRepository.FindAsync(suscripcion =>
            suscripcion.UsuarioId == usuario.Id && suscripcion.Activa);

        foreach (var suscripcion in vencidas)
        {
            suscripcion.Activa = false;
            await _suscripcionRepository.UpdateAsync(suscripcion);
        }

        var contratos = await _contratoRepository.FindAsync(contrato =>
            contrato.UsuarioId == usuario.Id && contrato.Estado == EstadoContratoSub.Activo);

        foreach (var contrato in contratos)
        {
            if (contrato.FechaFin is not null && contrato.FechaFin <= ahora)
            {
                contrato.Estado = EstadoContratoSub.Vencido;
                await _contratoRepository.UpdateAsync(contrato);
            }
        }

        usuario.Rol = RolUsuario.Basico;
        await _usuarioRepository.UpdateAsync(usuario);
    }
}
