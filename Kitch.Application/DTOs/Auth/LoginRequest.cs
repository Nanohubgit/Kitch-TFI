using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Credenciales de primer factor. <see cref="UsuarioOMail"/> acepta email o nombre de usuario.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email o NombreUsuario del titular de la cuenta.
    /// </summary>
    [Required(ErrorMessage = "Usuario o email es obligatorio.")]
    public string UsuarioOMail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}
