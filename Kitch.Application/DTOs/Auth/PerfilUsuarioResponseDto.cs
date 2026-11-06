namespace Kitch.Application.DTOs.Auth;

public class PerfilUsuarioResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public DateTime? SuscripcionActivaHasta { get; set; }

    public int PeticionesIaDelDia { get; set; }

    public int LimitePeticionesIa { get; set; }

    public string PreferenciaDietetica { get; set; } = string.Empty;
}
