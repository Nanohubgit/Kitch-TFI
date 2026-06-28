using Kitch.Application.DTOs.Usuarios;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

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
            // El rol nunca lo elige el cliente: se fuerza a Básico. Para ascender se usa
            // el endpoint admin-only CambiarRolAsync (mitigación de escalada de privilegios).
            Rol = RolUsuario.Basico
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
        // El rol NO se actualiza acá a propósito: cambiarlo es exclusivo del endpoint
        // admin-only CambiarRolAsync. Así el Update general no es una vía de escalada.

        await _repository.UpdateAsync(existingUsuario);

        return true;
    }

    public async Task<bool> ActualizarPerfilAsync(int usuarioId, ActualizarPerfilDto perfil)
    {
        var existingUsuario = await _repository.GetByIdAsync(usuarioId);

        if (existingUsuario is null)
        {
            return false;
        }

        var email = perfil.Email.Trim();

        if (await _repository.AnyAsync(existing => existing.Id != usuarioId && existing.Email == email))
        {
            throw new InvalidOperationException("El email ya se encuentra registrado.");
        }

        // El usuario solo puede tocar sus datos personales. Activo y Rol quedan
        // fuera a propósito: son competencia exclusiva del Admin.
        existingUsuario.Nombre = perfil.Nombre.Trim();
        existingUsuario.Apellido = perfil.Apellido.Trim();
        existingUsuario.Email = email;

        await _repository.UpdateAsync(existingUsuario);

        return true;
    }

    public async Task<bool> CambiarRolAsync(int usuarioId, string nuevoRol)
    {
        var rol = nuevoRol?.Trim() ?? string.Empty;

        // Validamos contra la lista blanca de roles: nunca confiamos en un string arbitrario.
        if (!RolUsuario.EsValido(rol))
        {
            throw new InvalidOperationException($"El rol '{nuevoRol}' no es un rol válido.");
        }

        var usuario = await _repository.GetByIdAsync(usuarioId);

        if (usuario is null)
        {
            return false;
        }

        usuario.Rol = rol;
        await _repository.UpdateAsync(usuario);

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
