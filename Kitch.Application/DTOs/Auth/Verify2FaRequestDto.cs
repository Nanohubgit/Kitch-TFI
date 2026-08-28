using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Segundo factor del login: identificador (email o nombre de usuario) + código OTP de 6 dígitos.
/// </summary>
public class Verify2FaRequestDto
{
    /// <summary>
    /// Email o NombreUsuario de la cuenta (el mismo identificador usado en el login).
    /// </summary>
    [Required(ErrorMessage = "Usuario o email es obligatorio.")]
    public string UsuarioOMail { get; set; } = string.Empty;

    /// <summary>
    /// Código numérico de 6 dígitos enviado por correo.
    /// </summary>
    [Required(ErrorMessage = "El código de verificación es obligatorio.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener 6 dígitos.")]
    public string Codigo { get; set; } = string.Empty;
}
