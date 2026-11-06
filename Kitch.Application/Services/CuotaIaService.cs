using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class CuotaIaService : ICuotaIaService
{
    private readonly IRepository<Usuario> _usuarioRepository;

    public CuotaIaService(IRepository<Usuario> usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task ConsumirAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new UnauthorizedAccessException("El usuario no existe.");

        var hoy = FechaLocal.Hoy();
        if (usuario.FechaCuotaIaUtc?.Date != hoy)
        {
            usuario.FechaCuotaIaUtc = hoy;
            usuario.PeticionesIaDelDia = 0;
        }

        var tope = PedidoRecetaPremium.TopeDiario(usuario.Rol);
        if (usuario.PeticionesIaDelDia >= tope)
        {
            throw new ForbiddenException(PedidoRecetaPremium.MensajeTopeDiario(usuario.Rol));
        }

        usuario.PeticionesIaDelDia++;
        await _usuarioRepository.UpdateAsync(usuario);
    }

    public async Task DevolverAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is null ||
            usuario.FechaCuotaIaUtc?.Date != FechaLocal.Hoy() ||
            usuario.PeticionesIaDelDia <= 0)
        {
            return;
        }

        usuario.PeticionesIaDelDia--;
        await _usuarioRepository.UpdateAsync(usuario);
    }
}
