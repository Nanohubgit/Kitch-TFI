using Kitch.Application.DTOs.Auth;

namespace Kitch.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<bool> EmailExisteAsync(string email);
}
