using Kitch.Application.DTOs.StockUsuarios;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class StockUsuarioService : IStockUsuarioService
{
    private readonly IRepository<StockUsuario> _repository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IIngredienteNormalizerService _normalizer;

    public StockUsuarioService(
        IRepository<StockUsuario> repository,
        IRepository<Ingrediente> ingredienteRepository,
        IIngredienteNormalizerService normalizer)
    {
        _repository = repository;
        _ingredienteRepository = ingredienteRepository;
        _normalizer = normalizer;
    }

    public async Task<IEnumerable<StockUsuarioResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var stock = await _repository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId,
            item => item.Ingrediente);
        return stock.Select(item => item.ToResponseDto());
    }

    public async Task<StockUsuarioResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var stock = await _repository.FindWithIncludesAsync(
            item => item.Id == id && item.UsuarioId == usuarioId,
            item => item.Ingrediente);
        return stock.FirstOrDefault()?.ToResponseDto();
    }

    public async Task<StockUsuarioResponseDto> CreateAsync(StockUsuarioCreateDto stock)
    {
        ValidateCantidad(stock.Cantidad);

        var ingredienteId = await ResolverIngredienteIdAsync(stock.IngredienteId, stock.NombreIngrediente);

        var stockExistente = await _repository.FirstOrDefaultAsync(existente =>
            existente.UsuarioId == stock.UsuarioId &&
            existente.IngredienteId == ingredienteId);

        if (stockExistente is not null)
        {
            stockExistente.Cantidad += stock.Cantidad;
            await _repository.UpdateAsync(stockExistente);

            return stockExistente.ToResponseDto();
        }

        var entity = new StockUsuario
        {
            UsuarioId = stock.UsuarioId,
            IngredienteId = ingredienteId,
            Cantidad = stock.Cantidad,
            UnidadMedida = stock.UnidadMedida.Trim(),
            FechaCaducidad = stock.FechaCaducidad
        };

        var created = await _repository.AddAsync(entity);

        return (await GetByIdAsync(created.Id, stock.UsuarioId))!;
    }

    private async Task<int> ResolverIngredienteIdAsync(int ingredienteId, string? nombreIngrediente)
    {
        if (ingredienteId > 0)
        {
            var existente = await _ingredienteRepository.GetByIdAsync(ingredienteId);
            if (existente is null)
            {
                throw new InvalidOperationException($"El ingrediente con id {ingredienteId} no existe en el catálogo.");
            }

            return ingredienteId;
        }

        if (!string.IsNullOrWhiteSpace(nombreIngrediente))
        {
            var nombre = _normalizer.Normalizar(nombreIngrediente);
            var existentePorNombre = await _ingredienteRepository.FirstOrDefaultAsync(
                ingrediente => ingrediente.Nombre == nombre);

            if (existentePorNombre is not null)
            {
                return existentePorNombre.Id;
            }

            var creado = await _ingredienteRepository.AddAsync(new Ingrediente
            {
                Nombre = nombre,
                Categoria = "Varios"
            });

            return creado.Id;
        }

        throw new InvalidOperationException("Debés indicar el ingrediente por id o por nombre.");
    }

    public async Task<bool> UpdateAsync(int id, StockUsuarioUpdateDto stock, int usuarioId)
    {
        ValidateCantidad(stock.Cantidad);

        var existingStock = await _repository.GetByIdAsync(id);

        if (existingStock is null || existingStock.UsuarioId != usuarioId)
        {
            return false;
        }

        existingStock.Cantidad = stock.Cantidad;
        existingStock.UnidadMedida = stock.UnidadMedida.Trim();

        await _repository.UpdateAsync(existingStock);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int usuarioId)
    {
        var stock = await _repository.GetByIdAsync(id);

        if (stock is null || stock.UsuarioId != usuarioId)
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
