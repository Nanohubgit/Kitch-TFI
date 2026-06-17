using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<Usuario> _repository;

    public AuthService(IRepository<Usuario> repository)
    {
        _repository = repository;
    }

    public async Task<Usuario> RegisterAsync(Usuario usuario)
    {
        if (await EmailExisteAsync(usuario.Email))
        {
            throw new InvalidOperationException("El email ya se encuentra registrado.");
        }

        return await _repository.AddAsync(usuario);
    }

    public async Task<Usuario?> LoginAsync(string email, string contrasena)
    {
        return await _repository.FirstOrDefaultAsync(usuario =>
            usuario.Email == email && usuario.Contrasena == contrasena);
    }

    public async Task<bool> EmailExisteAsync(string email)
    {
        return await _repository.AnyAsync(usuario => usuario.Email == email);
    }
}
