using Kitch.Application.DTOs.Usuarios;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IRepository<Usuario> _repository;

    public UsuarioService(IRepository<Usuario> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync()
    {
        var usuarios = await _repository.GetAllAsync();
        return usuarios.Select(usuario => usuario.ToResponseDto());
    }

    public async Task<UsuarioResponseDto?> GetByIdAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id);
        return usuario?.ToResponseDto();
    }

    public async Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto usuario)
    {
        var email = usuario.Email.Trim();

        if (await _repository.AnyAsync(existing => existing.Email == email))
        {
            throw new InvalidOperationException("El email ya se encuentra registrado.");
        }

        var entity = new Usuario
        {
            Nombre = usuario.Nombre.Trim(),
            Apellido = usuario.Apellido.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.Password),
            Activo = true,
            Rol = usuario.Rol.Trim()
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, UsuarioUpdateDto usuario)
    {
        var existingUsuario = await _repository.GetByIdAsync(id);

        if (existingUsuario is null)
        {
            return false;
        }

        var email = usuario.Email.Trim();

        if (await _repository.AnyAsync(existing => existing.Id != id && existing.Email == email))
        {
            throw new InvalidOperationException("El email ya se encuentra registrado.");
        }

        existingUsuario.Nombre = usuario.Nombre.Trim();
        existingUsuario.Apellido = usuario.Apellido.Trim();
        existingUsuario.Email = email;
        existingUsuario.Activo = usuario.Activo;
        existingUsuario.Rol = usuario.Rol.Trim();

        await _repository.UpdateAsync(existingUsuario);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id);

        if (usuario is null)
        {
            return false;
        }

        await _repository.DeleteAsync(usuario);

        return true;
    }
}
