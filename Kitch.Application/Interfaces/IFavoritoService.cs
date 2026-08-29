using Kitch.Application.DTOs.Favoritos;

namespace Kitch.Application.Interfaces;

public interface IFavoritoService
{
    Task<IEnumerable<FavoritoResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<FavoritoResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<FavoritoResponseDto> AddFavoritoAsync(FavoritoCreateDto favorito);
    Task<bool> ToggleFavoritoAsync(int usuarioId, int recetaId);
    Task<bool> DeleteAsync(int id, int usuarioId);
    Task<bool> ExisteFavoritoAsync(int usuarioId, int recetaId);
    Task AsegurarCupoFavoritosAsync(int usuarioId);
}
