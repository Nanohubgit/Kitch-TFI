using Kitch.Application.DTOs.Favoritos;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class FavoritoService : IFavoritoService
{
    private readonly IRepository<RecetaFavorita> _repository;

    public FavoritoService(IRepository<RecetaFavorita> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FavoritoResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var favoritos = await _repository.FindWithIncludesAsync(
            favorito => favorito.UsuarioId == usuarioId,
            favorito => favorito.Usuario,
            favorito => favorito.Receta);

        return favoritos.Select(favorito => favorito.ToResponseDto());
    }

    public async Task<FavoritoResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var favoritos = await _repository.FindWithIncludesAsync(
            favorito => favorito.Id == id && favorito.UsuarioId == usuarioId,
            favorito => favorito.Usuario,
            favorito => favorito.Receta);

        return favoritos.FirstOrDefault()?.ToResponseDto();
    }

    public async Task<FavoritoResponseDto> AddFavoritoAsync(FavoritoCreateDto favorito)
    {
        if (await ExisteFavoritoAsync(favorito.UsuarioId, favorito.RecetaId))
        {
            throw new InvalidOperationException("La receta ya esta marcada como favorita para este usuario.");
        }

        var entity = new RecetaFavorita
        {
            UsuarioId = favorito.UsuarioId,
            RecetaId = favorito.RecetaId
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> ToggleFavoritoAsync(int usuarioId, int recetaId)
    {
        var favoritoExistente = await _repository.FirstOrDefaultAsync(favorito =>
            favorito.UsuarioId == usuarioId && favorito.RecetaId == recetaId);

        if (favoritoExistente is not null)
        {
            await _repository.DeleteAsync(favoritoExistente);

            return false;
        }

        var nuevoFavorito = new RecetaFavorita
        {
            UsuarioId = usuarioId,
            RecetaId = recetaId
        };

        await _repository.AddAsync(nuevoFavorito);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int usuarioId)
    {
        var favorito = await _repository.GetByIdAsync(id);

        // Solo podés borrar tus propios favoritos.
        if (favorito is null || favorito.UsuarioId != usuarioId)
        {
            return false;
        }

        await _repository.DeleteAsync(favorito);

        return true;
    }

    public async Task<bool> ExisteFavoritoAsync(int usuarioId, int recetaId)
    {
        return await _repository.AnyAsync(favorito =>
            favorito.UsuarioId == usuarioId && favorito.RecetaId == recetaId);
    }
}
