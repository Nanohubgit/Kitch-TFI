using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface ISuscripcionVigenciaService
{
    Task AsegurarRolVigenteAsync(int usuarioId);

    Task<bool> AsegurarUsuarioHabilitadoAsync(int usuarioId);

    Task<string?> ObtenerRolVigenteAsync(int usuarioId);

    Task AsegurarRolVigenteAsync(Usuario usuario);
}
