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

    public StockUsuarioService(
        IRepository<StockUsuario> repository,
        IRepository<Ingrediente> ingredienteRepository)
    {
        _repository = repository;
        _ingredienteRepository = ingredienteRepository;
    }

    public async Task<IEnumerable<StockUsuarioResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var stock = await _repository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId,
            item => item.Ingrediente);
        return stock.Select(item => item.ToResponseDto());
    }

    public async Task<StockUsuarioResponseDto?> GetByIdAsync(int id)
    {
        var stock = await _repository.FindWithIncludesAsync(
            item => item.Id == id,
            item => item.Ingrediente);
        return stock.FirstOrDefault()?.ToResponseDto();
    }

    public async Task<StockUsuarioResponseDto> CreateAsync(StockUsuarioCreateDto stock)
    {
        ValidateCantidad(stock.Cantidad);

        // Resolvemos el ingrediente: por id (si vino) o por nombre (lo busca o lo crea).
        var ingredienteId = await ResolverIngredienteIdAsync(stock.IngredienteId, stock.NombreIngrediente);

        var stockExistente = await _repository.FirstOrDefaultAsync(existente =>
            existente.UsuarioId == stock.UsuarioId &&
            existente.IngredienteId == ingredienteId);

        // Si el usuario ya tenía ese ingrediente, sumamos la cantidad en lugar de duplicar.
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
            UnidadMedida = stock.UnidadMedida.Trim()
        };

        var created = await _repository.AddAsync(entity);

        // Releemos con el ingrediente incluido para devolver el nombre en la respuesta.
        return (await GetByIdAsync(created.Id))!;
    }

    private async Task<int> ResolverIngredienteIdAsync(int ingredienteId, string? nombreIngrediente)
    {
        // Caso A: vino un id explícito -> validamos que exista en el catálogo.
        if (ingredienteId > 0)
        {
            var existente = await _ingredienteRepository.GetByIdAsync(ingredienteId);
            if (existente is null)
            {
                throw new InvalidOperationException($"El ingrediente con id {ingredienteId} no existe en el catálogo.");
            }

            return ingredienteId;
        }

        // Caso B: vino un nombre -> lo buscamos o lo creamos.
        if (!string.IsNullOrWhiteSpace(nombreIngrediente))
        {
            var nombre = nombreIngrediente.Trim();
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
