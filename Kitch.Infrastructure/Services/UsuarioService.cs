using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class UsuarioService : IUsuarioService
{
    private readonly KitchDbContext _context;

    public UsuarioService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        return await _context.Usuarios.ToListAsync();
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        return await _context.Usuarios.FindAsync(id);
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<bool> UpdateAsync(int id, Usuario usuario)
    {
        var existingUsuario = await _context.Usuarios.FindAsync(id);

        if (existingUsuario is null)
        {
            return false;
        }

        usuario.Id = id;
        _context.Entry(existingUsuario).CurrentValues.SetValues(usuario);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario is null)
        {
            return false;
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        return true;
    }
}
