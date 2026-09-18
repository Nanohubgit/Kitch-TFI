using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

/// <summary>
/// Si el mes de Profesional venció, baja el rol a Básico y marca contratos/suscripciones.
/// </summary>
public interface ISuscripcionVigenciaService
{
    Task AsegurarRolVigenteAsync(int usuarioId);

    Task AsegurarRolVigenteAsync(Usuario usuario);
}
