namespace Kitch.Domain.Constants;

/// <summary>
/// Detecta si el usuario está pidiendo una receta de dificultad Profesional (Difícil)
/// para poder rechazar el pedido ANTES de gastar cuota de IA.
/// </summary>
public static class PedidoRecetaPremium
{
    public static bool EsPedido(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var t = texto.Trim().ToLowerInvariant();
        return t.Contains("dificil", StringComparison.Ordinal) ||
               t.Contains("difícil", StringComparison.Ordinal) ||
               t.Contains("profesional", StringComparison.Ordinal) ||
               t.Contains("avanzad", StringComparison.Ordinal);
    }

    public static int TopeDiario(string? rol) =>
        RolUsuario.TieneAccesoPremium(rol)
            ? LimitesPlan.MaxPeticionesIaProfesional
            : LimitesPlan.MaxPeticionesIaBasico;

    public static string MensajeTopeDiario(string? rol) =>
        RolUsuario.TieneAccesoPremium(rol)
            ? LimitesPlan.MensajeLimiteIaProfesional
            : LimitesPlan.MensajeLimiteIaBasico;
}
