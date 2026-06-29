using Kitch.Application.DTOs.Favoritos;

namespace Kitch.Application.Interfaces;

public interface IFavoritoService
{
    Task<IEnumerable<FavoritoResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    // GetById y Delete reciben el usuarioId del token para verificar que el favorito
    // le pertenezca (evita IDOR: ver/borrar favoritos de otro usuario).
    Task<FavoritoResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<FavoritoResponseDto> AddFavoritoAsync(FavoritoCreateDto favorito);
    Task<bool> ToggleFavoritoAsync(int usuarioId, int recetaId);
    Task<bool> DeleteAsync(int id, int usuarioId);
    Task<bool> ExisteFavoritoAsync(int usuarioId, int recetaId);
}
