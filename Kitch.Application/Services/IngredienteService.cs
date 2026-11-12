using Kitch.Application.DTOs.Ingredientes;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Kitch.Application.Services;

public class IngredienteService : IIngredienteService
{
    private readonly IRepository<Ingrediente> _repository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<IngredienteReceta> _ingredienteRecetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<SustitutoIngrediente> _sustitutoRepository;
    private readonly IRepository<ItemListaCompra> _itemListaRepository;
    private readonly IIngredienteNormalizerService _normalizer;
    private readonly IMemoryCache _cache;

    public IngredienteService(
        IRepository<Ingrediente> repository,
        IRepository<Usuario> usuarioRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<SustitutoIngrediente> sustitutoRepository,
        IRepository<ItemListaCompra> itemListaRepository,
        IIngredienteNormalizerService normalizer,
        IMemoryCache cache)
    {
        _itemListaRepository = itemListaRepository;
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _ingredienteRecetaRepository = ingredienteRecetaRepository;
        _stockRepository = stockRepository;
        _sustitutoRepository = sustitutoRepository;
        _normalizer = normalizer;
        _cache = cache;
    }

    public async Task<IEnumerable<IngredienteResponseDto>> GetAllAsync(int usuarioId)
    {
        if (await EsAdminAsync(usuarioId))
        {
            var todos = await _repository.GetAllAsync();
            return todos.Select(ingrediente => ingrediente.ToResponseDto());
        }

        var ids = await ObtenerIdsAccesiblesAsync(usuarioId);
        var ingredientes = await _repository.FindAsync(ingrediente => ids.Contains(ingrediente.Id));
        return ingredientes.Select(ingrediente => ingrediente.ToResponseDto());
    }

    public async Task<IngredienteResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        if (!await EsAdminAsync(usuarioId) && !(await ObtenerIdsAccesiblesAsync(usuarioId)).Contains(id))
        {
            return null;
        }

        var ingrediente = await _repository.GetByIdAsync(id);
        return ingrediente?.ToResponseDto();
    }

    public async Task<HashSet<int>> ObtenerIdsAccesiblesAsync(int usuarioId)
    {
        var enStock = await _stockRepository.FindAsync(stock => stock.UsuarioId == usuarioId);
        var enRecetas = await _ingredienteRecetaRepository.FindAsync(item => item.Receta.UsuarioId == null || item.Receta.UsuarioId == usuarioId);
        var enLista = await _itemListaRepository.FindAsync(item => item.UsuarioId == usuarioId && item.IngredienteId != null);

        var ids = enStock.Select(stock => stock.IngredienteId).ToHashSet();
        ids.UnionWith(enRecetas.Select(item => item.IngredienteId));
        ids.UnionWith(enLista.Select(item => item.IngredienteId!.Value));
        return ids;
    }

    private async Task<bool> EsAdminAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        return usuario?.Rol == RolUsuario.Admin;
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
        _cache.Remove(RecetaService.CacheCatalogoKey);

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

        if (await _ingredienteRecetaRepository.AnyAsync(item => item.IngredienteId == id))
        {
            throw new ConflictException("No se puede eliminar el ingrediente porque lo usan una o más recetas.");
        }

        if (await _stockRepository.AnyAsync(stock => stock.IngredienteId == id))
        {
            throw new ConflictException("No se puede eliminar el ingrediente porque hay usuarios que lo tienen en su alacena.");
        }

        if (await _sustitutoRepository.AnyAsync(sustituto =>
                sustituto.IngredienteOriginalId == id || sustituto.IngredienteSustitutoId == id))
        {
            throw new ConflictException("No se puede eliminar el ingrediente porque tiene sustitutos registrados. Eliminá primero esos sustitutos.");
        }

        await _repository.DeleteAsync(ingrediente);
        _cache.Remove(RecetaService.CacheCatalogoKey);

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
