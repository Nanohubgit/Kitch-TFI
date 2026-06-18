using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class StockUsuarioService : IStockUsuarioService
{
    private readonly IRepository<StockUsuario> _repository;

    public StockUsuarioService(IRepository<StockUsuario> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<StockUsuario>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _repository.FindAsync(stock => stock.UsuarioId == usuarioId);
    }

    public async Task<StockUsuario?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<StockUsuario> CreateAsync(StockUsuario stock)
    {
        if (stock.Cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a cero");
        }

        var stockExistente = await _repository.FirstOrDefaultAsync(existente =>
            existente.UsuarioId == stock.UsuarioId &&
            existente.IngredienteId == stock.IngredienteId);

        if (stockExistente is not null)
        {
            stockExistente.Cantidad += stock.Cantidad;
            await _repository.UpdateAsync(stockExistente);

            return stockExistente;
        }

        return await _repository.AddAsync(stock);
    }

    public async Task<bool> UpdateAsync(int id, StockUsuario stock)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        stock.Id = id;
        await _repository.UpdateAsync(stock);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var stock = await _repository.GetByIdAsync(id);

        if (stock is null)
        {
            return false;
        }

        await _repository.DeleteAsync(stock);

        return true;
    }
}
