using Kitch.Application.Interfaces;
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

    public async Task<IEnumerable<RecetaFavorita>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _repository.FindAsync(favorito => favorito.UsuarioId == usuarioId);
    }

    public async Task<RecetaFavorita?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<RecetaFavorita> AddFavoritoAsync(RecetaFavorita favorito)
    {
        if (await ExisteFavoritoAsync(favorito.UsuarioId, favorito.RecetaId))
        {
            throw new InvalidOperationException("La receta ya está marcada como favorita para este usuario.");
        }

        return await _repository.AddAsync(favorito);
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

    public async Task<bool> DeleteAsync(int id)
    {
        var favorito = await _repository.GetByIdAsync(id);

        if (favorito is null)
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
