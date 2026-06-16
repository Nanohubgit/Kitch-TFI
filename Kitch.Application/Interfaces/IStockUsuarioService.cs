using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IStockUsuarioService
{
    Task<IEnumerable<StockUsuario>> GetByUsuarioIdAsync(int usuarioId);
    Task<StockUsuario?> GetByIdAsync(int id);
    Task<StockUsuario> CreateAsync(StockUsuario stock);
    Task<bool> UpdateAsync(int id, StockUsuario stock);
    Task<bool> DeleteAsync(int id);
}
