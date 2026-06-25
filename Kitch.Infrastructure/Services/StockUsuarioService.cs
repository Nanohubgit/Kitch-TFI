using Kitch.Application.DTOs.StockUsuarios;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class StockUsuarioService : IStockUsuarioService
{
    private readonly IRepository<StockUsuario> _repository;

    public StockUsuarioService(IRepository<StockUsuario> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<StockUsuarioResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var stock = await _repository.FindAsync(item => item.UsuarioId == usuarioId);
        return stock.Select(item => item.ToResponseDto());
    }

    public async Task<StockUsuarioResponseDto?> GetByIdAsync(int id)
    {
        var stock = await _repository.GetByIdAsync(id);
        return stock?.ToResponseDto();
    }

    public async Task<StockUsuarioResponseDto> CreateAsync(StockUsuarioCreateDto stock)
    {
        ValidateCantidad(stock.Cantidad);

        if (await _repository.AnyAsync(existing =>
                existing.UsuarioId == stock.UsuarioId && existing.IngredienteId == stock.IngredienteId))
        {
            throw new InvalidOperationException("El ingrediente ya existe en el stock del usuario.");
        }

        var entity = new StockUsuario
        {
            UsuarioId = stock.UsuarioId,
            IngredienteId = stock.IngredienteId,
            Cantidad = stock.Cantidad,
            UnidadMedida = stock.UnidadMedida.Trim()
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, StockUsuarioUpdateDto stock)
    {
        ValidateCantidad(stock.Cantidad);

        var existingStock = await _repository.GetByIdAsync(id);

        if (existingStock is null)
        {
            return false;
        }

        existingStock.Cantidad = stock.Cantidad;
        existingStock.UnidadMedida = stock.UnidadMedida.Trim();

        await _repository.UpdateAsync(existingStock);

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

    private static void ValidateCantidad(decimal cantidad)
    {
        if (cantidad < 0)
        {
            throw new InvalidOperationException("La cantidad disponible no puede ser negativa.");
        }
    }
}
