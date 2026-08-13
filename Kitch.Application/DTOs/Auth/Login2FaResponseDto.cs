namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Respuesta del login en el paso 1 del 2FA.
/// Confirma que la password fue correcta y que se envió un código por email.
/// No incluye JWT: la sesión se emite solo tras verificar el código (Fase 3).
/// </summary>
public class Login2FaResponseDto
{
    /// <summary>
    /// Siempre true en este paso: el cliente debe pedir el segundo factor.
    /// </summary>
    public bool RequiresTwoFactor { get; set; } = true;

    /// <summary>
    /// Email real de la cuenta (el front lo reenvía en verify-2fa).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Email enmascarado para mostrar en UI (ej: a***@mail.com).
    /// </summary>
    public string EmailEnmascarado { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje orientativo para la UI / Swagger.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;
}
