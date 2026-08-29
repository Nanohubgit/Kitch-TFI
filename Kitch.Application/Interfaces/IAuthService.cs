using Kitch.Application.DTOs.Auth;

namespace Kitch.Application.Interfaces;

/// <summary>
/// Contrato de autenticación, 2FA, recuperación de acceso y perfil.
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
    /// Valida el código 2FA, limpia el OTP y emite el JWT definitivo.
    /// </summary>
    Task<Verify2FaResponseDto> Verify2FaAsync(Verify2FaRequestDto request);

    /// <summary>
    /// Perfil del usuario autenticado (panel / navbar del front).
    /// </summary>
    Task<PerfilUsuarioResponseDto?> GetPerfilAsync(int usuarioId);

    /// <summary>
    /// Actualiza NombreUsuario y PreferenciaDietetica del usuario autenticado.
    /// </summary>
    Task<PerfilUsuarioResponseDto> EditarPerfilAsync(int usuarioId, EditarPerfilRequestDto request);

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
