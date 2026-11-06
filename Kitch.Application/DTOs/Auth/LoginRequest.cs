using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Usuario o email es obligatorio.")]
    public string UsuarioOMail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}
