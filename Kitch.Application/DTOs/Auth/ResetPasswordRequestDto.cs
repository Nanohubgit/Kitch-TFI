using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Solicitud de confirmación del reseteo: el token en texto plano (del email)
/// y la nueva contraseña. El backend hashea el token y lo compara con <c>Usuario.PasswordResetTokenHash</c>.
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>
    /// Token de recuperación en texto plano, tal como llegó al usuario por email (nunca se persiste así en DB).
    /// </summary>
    [Required(ErrorMessage = "El token de recuperación es obligatorio.")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Nueva contraseña elegida por el usuario. Se almacenará solo como hash BCrypt en <c>Usuario.PasswordHash</c>.
    /// </summary>
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
    public string NuevaPassword { get; set; } = string.Empty;
}
