using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IAuthService
{
    Task<Usuario> RegisterAsync(Usuario usuario);
    Task<Usuario?> LoginAsync(string email, string contrasena);
    Task<bool> EmailExisteAsync(string email);
}
