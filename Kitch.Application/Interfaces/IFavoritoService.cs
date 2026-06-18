using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IFavoritoService
{
    Task<IEnumerable<RecetaFavorita>> GetByUsuarioIdAsync(int usuarioId);
    Task<RecetaFavorita?> GetByIdAsync(int id);
    Task<RecetaFavorita> AddFavoritoAsync(RecetaFavorita favorito);
    Task<bool> ToggleFavoritoAsync(int usuarioId, int recetaId);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExisteFavoritoAsync(int usuarioId, int recetaId);
}
