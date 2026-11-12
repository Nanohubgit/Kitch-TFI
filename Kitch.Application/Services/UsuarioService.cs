using Kitch.Application.DTOs.Usuarios;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Kitch.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IRepository<Usuario> _repository;
    private readonly IRepository<Pago> _pagoRepository;
    private readonly IRepository<ContratoSub> _contratoRepository;
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IMemoryCache _cache;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IPasswordHasher _passwordHasher;

    public UsuarioService(
        IRepository<Usuario> repository,
        IRepository<Pago> pagoRepository,
        IRepository<ContratoSub> contratoRepository,
        IRepository<Receta> recetaRepository,
        IMemoryCache cache,
        IUnidadDeTrabajo unidadDeTrabajo,
        IPasswordHasher passwordHasher)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _passwordHasher = passwordHasher;
        _repository = repository;
        _pagoRepository = pagoRepository;
        _contratoRepository = contratoRepository;
        _recetaRepository = recetaRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var usuarios = await _repository.GetAllAsync();
        return usuarios.Select(usuario => usuario.ToResponseDto());
    }

    public async Task<UsuarioResponseDto?> GetByIdAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id);
        return usuario?.ToResponseDto();
    }

    public async Task<UsuarioResponseDto?> GetByIdAdminAsync(int id, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var usuario = await _repository.GetByIdAsync(id);
        return usuario?.ToResponseDto();
    }

    public async Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto usuario, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var email = usuario.Email.Trim();

        if (await _repository.AnyAsync(existing => existing.Email == email))
        {
            throw new InvalidOperationException("El email ya se encuentra registrado.");
        }

        var nombreUsuario = usuario.NombreUsuario.Trim();
        if (await _repository.AnyAsync(existing => existing.NombreUsuario == nombreUsuario))
        {
            throw new InvalidOperationException("El nombre de usuario ya está en uso.");
        }

        var entity = new Usuario
        {
            Nombre = usuario.Nombre.Trim(),
            Apellido = usuario.Apellido.Trim(),
            NombreUsuario = nombreUsuario,
            Email = email,
            PreferenciaDietetica = string.IsNullOrWhiteSpace(usuario.PreferenciaDietetica)
                ? "Ninguna"
                : usuario.PreferenciaDietetica.Trim(),
            PasswordHash = _passwordHasher.Hash(usuario.Password),
            Activo = true,
            Rol = RolUsuario.Basico
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, UsuarioUpdateDto usuario, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var existingUsuario = await _repository.GetByIdAsync(id);

        if (existingUsuario is null)
        {
            return false;
        }

        if (existingUsuario.Rol == RolUsuario.Admin && id != solicitanteId)
        {
            throw new ForbiddenException("No se pueden modificar los datos de otro administrador.");
        }

        if (!usuario.Activo && existingUsuario.Activo && (id == solicitanteId || existingUsuario.Rol == RolUsuario.Admin))
        {
            throw new InvalidOperationException("No se puede desactivar una cuenta de administrador.");
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
        if (!usuario.Activo)
        {
            existingUsuario.TwoFactorCode = null;
            existingUsuario.TwoFactorCodeExpiresAt = null;
            existingUsuario.PasswordResetTokenHash = null;
            existingUsuario.PasswordResetTokenExpiresAt = null;
        }

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

        if (!string.Equals(existingUsuario.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El email no se puede cambiar desde el perfil porque es el que recibe los códigos de verificación.");
        }

        existingUsuario.Nombre = perfil.Nombre.Trim();
        existingUsuario.Apellido = perfil.Apellido.Trim();
        if (!string.IsNullOrWhiteSpace(perfil.PreferenciaDietetica))
        {
            existingUsuario.PreferenciaDietetica = perfil.PreferenciaDietetica.Trim();
        }

        await _repository.UpdateAsync(existingUsuario);

        return true;
    }

    public async Task<bool> CambiarRolAsync(int usuarioId, string nuevoRol, int adminEjecutorId)
    {
        await ValidarPermisosAdminAsync(adminEjecutorId);

        var rol = nuevoRol?.Trim() ?? string.Empty;

        if (!RolUsuario.EsValido(rol))
        {
            throw new InvalidOperationException($"El rol '{nuevoRol}' no es un rol válido.");
        }

        var usuario = await _repository.GetByIdAsync(usuarioId);

        if (usuario is null)
        {
            return false;
        }

        if (rol == usuario.Rol)
        {
            return true;
        }

        if (rol == RolUsuario.Admin && usuario.Rol != RolUsuario.Admin)
        {
            throw new ForbiddenException(
                "El rol Admin no se asigna por esta vía. Queda reservado a las cuentas internas del sistema.");
        }

        if (rol == RolUsuario.Profesional)
        {
            throw new ForbiddenException(
                "El plan Profesional solo se obtiene con un pago aprobado en Mercado Pago. No se puede asignar manualmente.");
        }

        if (usuario.Rol == RolUsuario.Profesional)
        {
            throw new ForbiddenException(
                "No se puede quitar un plan Profesional pagado. Vence solo al terminar el período; si hay un problema con la cuenta, bloqueala.");
        }

        if (usuarioId == adminEjecutorId && rol != RolUsuario.Admin)
        {
            throw new InvalidOperationException("No podés cambiar tu propio rol de administrador.");
        }

        if (usuario.Rol == RolUsuario.Admin && rol != RolUsuario.Admin &&
            !await ExisteOtroAdminActivoAsync(usuarioId))
        {
            throw new InvalidOperationException(
                "No se puede degradar al último administrador activo del sistema.");
        }

        usuario.Rol = rol;
        await _repository.UpdateAsync(usuario);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int adminEjecutorId)
    {
        await ValidarPermisosAdminAsync(adminEjecutorId);

        var usuario = await _repository.GetByIdAsync(id);

        if (usuario is null)
        {
            return false;
        }

        if (id == adminEjecutorId)
        {
            throw new InvalidOperationException("No podés eliminar tu propia cuenta de administrador.");
        }

        if (usuario.Rol == RolUsuario.Admin)
        {
            throw new ForbiddenException("No se puede eliminar una cuenta de administrador.");
        }

        if (await _pagoRepository.AnyAsync(pago => pago.UsuarioId == id) ||
            await _contratoRepository.AnyAsync(contrato => contrato.UsuarioId == id))
        {
            throw new ConflictException(
                "El usuario tiene pagos o contratos registrados y el historial no se puede borrar. Bloqueá la cuenta en lugar de eliminarla.");
        }

        await _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            var recetas = await _recetaRepository.FindAsync(receta => receta.UsuarioId == id);
            foreach (var receta in recetas)
            {
                await _recetaRepository.DeleteAsync(receta);
            }

            await _repository.DeleteAsync(usuario);
        });

        _cache.Remove(RecetaService.CacheCatalogoKey);

        return true;
    }

    private async Task ValidarPermisosAdminAsync(int usuarioId)
    {
        var usuario = await _repository.GetByIdAsync(usuarioId);
        if (usuario is null)
        {
            throw new UnauthorizedAccessException();
        }

        if (usuario.Rol != RolUsuario.Admin)
        {
            throw new ForbiddenException(
                "Acceso denegado. Se requieren permisos de administrador para visualizar esta información.");
        }
    }

    private async Task<bool> ExisteOtroAdminActivoAsync(int excluirUsuarioId) =>
        await _repository.AnyAsync(u =>
            u.Id != excluirUsuarioId && u.Rol == RolUsuario.Admin && u.Activo);
}
