namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Respuesta del login en el paso 1 del 2FA.
/// Confirma que la password fue correcta y que se envió un código por email.
/// No incluye JWT: la sesión se emite solo tras POST /api/auth/verify-2fa.
/// </summary>
public class Login2FaResponseDto
{
    /// <summary>
    /// Siempre true en este paso: el cliente debe pedir el segundo factor.
    /// </summary>
    public bool RequiresTwoFactor { get; set; } = true;

    /// <summary>
    /// Email enmascarado donde se envió el código (ej: a***@mail.com).
    /// </summary>
    public string EmailEnmascarado { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje orientativo para la UI / Swagger.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;
}
