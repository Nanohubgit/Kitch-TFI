using Kitch.Application.DTOs.StockUsuarios;

namespace Kitch.Application.Interfaces;

public interface IStockUsuarioService
{
    Task<IEnumerable<StockUsuarioResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<StockUsuarioResponseDto?> GetByIdAsync(int id);
    Task<StockUsuarioResponseDto> CreateAsync(StockUsuarioCreateDto stock);
    Task<bool> UpdateAsync(int id, StockUsuarioUpdateDto stock);
    Task<bool> DeleteAsync(int id);
}
