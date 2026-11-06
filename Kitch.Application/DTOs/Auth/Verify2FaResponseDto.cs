namespace Kitch.Application.DTOs.Auth;

public class Verify2FaResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Mensaje { get; set; } = "Login exitoso";
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
