using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Segundo factor del login: email de la cuenta + código de 6 dígitos del mail.
/// </summary>
public class Verify2FaRequestDto
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Código OTP de 6 dígitos recibido por correo.
    /// </summary>
    [Required(ErrorMessage = "El código de verificación es obligatorio.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener exactamente 6 dígitos.")]
    public string Codigo { get; set; } = string.Empty;
}
