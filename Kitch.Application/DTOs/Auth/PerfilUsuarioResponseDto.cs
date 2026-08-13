namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Datos del usuario logueado para el panel de perfil / menú del frontend.
/// </summary>
public class PerfilUsuarioResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    /// <summary>
    /// Basico | Profesional | Admin — el front usa esto para mostrar UI Premium / admin.
    /// </summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>
    /// Fin de la suscripción activa, si existe. Null = sin premium vigente.
    /// </summary>
    public DateTime? SuscripcionActivaHasta { get; set; }

    public string PreferenciaDietetica { get; set; } = string.Empty;
}
