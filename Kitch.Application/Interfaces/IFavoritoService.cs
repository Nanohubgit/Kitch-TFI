using Kitch.Application.DTOs.Favoritos;

namespace Kitch.Application.Interfaces;

public interface IFavoritoService
{
    Task<IEnumerable<FavoritoResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<FavoritoResponseDto?> GetByIdAsync(int id);
    Task<FavoritoResponseDto> AddFavoritoAsync(FavoritoCreateDto favorito);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExisteFavoritoAsync(int usuarioId, int recetaId);
}
