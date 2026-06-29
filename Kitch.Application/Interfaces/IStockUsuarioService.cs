using Kitch.Application.DTOs.StockUsuarios;

namespace Kitch.Application.Interfaces;

public interface IStockUsuarioService
{
    Task<IEnumerable<StockUsuarioResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    // GetById, Update y Delete reciben el usuarioId del token para verificar que el ítem
    // de stock le pertenezca (evita IDOR: ver/editar/borrar alacena de otro usuario).
    Task<StockUsuarioResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<StockUsuarioResponseDto> CreateAsync(StockUsuarioCreateDto stock);
    Task<bool> UpdateAsync(int id, StockUsuarioUpdateDto stock, int usuarioId);
    Task<bool> DeleteAsync(int id, int usuarioId);
}
