using Kitch.Application.DTOs.Auth;

namespace Kitch.Application.Interfaces;

/// <summary>
/// Contrato de autenticación y recuperación de acceso de la aplicación.
/// Los controladores dependen de esta abstracción (DIP), no de AuthService concreto.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registra un usuario nuevo y devuelve un JWT de sesión.
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Valida primer factor (usuario/mail + contraseña). No emite JWT:
    /// genera código 2FA, lo envía por email y devuelve <see cref="Login2FaResponseDto"/>.
    /// </summary>
    Task<Login2FaResponseDto> LoginAsync(LoginRequest request);

    /// <summary>
    /// Indica si el email ya está registrado.
    /// </summary>
    Task<bool> EmailExisteAsync(string email);

    /// <summary>
    /// Inicia la recuperación de contraseña: genera token, guarda su hash con expiración
    /// y envía el correo. Debe responder de forma genérica (sin filtrar si el email existe).
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequestDto request);

    /// <summary>
    /// Completa la recuperación: valida token (hash + expiración), actualiza el PasswordHash
    /// e invalida el token (un solo uso).
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordRequestDto request);
}
