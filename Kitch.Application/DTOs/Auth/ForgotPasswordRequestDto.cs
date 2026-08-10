using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Solicitud de inicio del flujo "olvidé mi contraseña".
/// Solo identifica al usuario por email; no autentica ni devuelve secretos.
/// </summary>
public class ForgotPasswordRequestDto
{
    /// <summary>
    /// Email de la cuenta a la que se enviará el enlace/token de recuperación (si existe y está activa).
    /// </summary>
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    public string Email { get; set; } = string.Empty;
}
