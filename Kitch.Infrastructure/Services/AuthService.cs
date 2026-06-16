using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly KitchDbContext _context;

    public AuthService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> RegisterAsync(Usuario usuario)
    {
        if (await EmailExisteAsync(usuario.Email))
        {
            throw new InvalidOperationException("El email ya se encuentra registrado.");
        }

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<Usuario?> LoginAsync(string email, string contrasena)
    {
        return await _context.Usuarios.FirstOrDefaultAsync(usuario =>
            usuario.Email == email && usuario.Contrasena == contrasena);
    }

    public async Task<bool> EmailExisteAsync(string email)
    {
        return await _context.Usuarios.AnyAsync(usuario => usuario.Email == email);
    }
}
