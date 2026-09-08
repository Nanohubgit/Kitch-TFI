using Kitch.Application.DTOs.Ingredientes;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class IngredienteService : IIngredienteService
{
    private readonly IRepository<Ingrediente> _repository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IIngredienteNormalizerService _normalizer;

    public IngredienteService(
        IRepository<Ingrediente> repository,
        IRepository<Usuario> usuarioRepository,
        IIngredienteNormalizerService normalizer)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _normalizer = normalizer;
    }

    public async Task<IEnumerable<IngredienteResponseDto>> GetAllAsync()
    {
        var ingredientes = await _repository.GetAllAsync();
        return ingredientes.Select(ingrediente => ingrediente.ToResponseDto());
    }

    public async Task<IngredienteResponseDto?> GetByIdAsync(int id)
    {
        var ingrediente = await _repository.GetByIdAsync(id);
        return ingrediente?.ToResponseDto();
    }

    public async Task<IngredienteResponseDto> CreateAsync(IngredienteCreateDto ingrediente, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var nombre = _normalizer.Normalizar(ingrediente.Nombre);

        if (await _repository.AnyAsync(existing => existing.Nombre == nombre))
        {
            throw new InvalidOperationException("Ya existe un ingrediente con ese nombre.");
        }

        var entity = new Ingrediente
        {
            Nombre = nombre,
            Descripcion = ingrediente.Descripcion?.Trim()
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, IngredienteUpdateDto ingrediente, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var existingIngrediente = await _repository.GetByIdAsync(id);

        if (existingIngrediente is null)
        {
            return false;
        }

        var nombre = _normalizer.Normalizar(ingrediente.Nombre);

        if (await _repository.AnyAsync(existing => existing.Id != id && existing.Nombre == nombre))
        {
            throw new InvalidOperationException("Ya existe un ingrediente con ese nombre.");
        }

        existingIngrediente.Nombre = nombre;
        existingIngrediente.Descripcion = ingrediente.Descripcion?.Trim();

        await _repository.UpdateAsync(existingIngrediente);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var ingrediente = await _repository.GetByIdAsync(id);

        if (ingrediente is null)
        {
            return false;
        }

        await _repository.DeleteAsync(ingrediente);

        return true;
    }

    private async Task ValidarPermisosAdminAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
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
}
