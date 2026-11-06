using Kitch.Application.DTOs.Auth;

namespace Kitch.Application.Interfaces;

public interface IAuthService
{
    Task<Login2FaResponseDto> RegisterAsync(RegisterRequest request);
    Task<Login2FaResponseDto> LoginAsync(LoginRequest request);
    Task<Verify2FaResponseDto> Verify2FaAsync(Verify2FaRequestDto request);
    Task<PerfilUsuarioResponseDto?> GetPerfilAsync(int usuarioId);
    Task<PerfilUsuarioResponseDto> EditarPerfilAsync(int usuarioId, EditarPerfilRequestDto request);
    Task<bool> EmailExisteAsync(string email);
    Task ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<Verify2FaResponseDto> RefrescarSesionAsync(int usuarioId);
    Task EnviarRecuperacionPasswordAsync(int usuarioId);
}
