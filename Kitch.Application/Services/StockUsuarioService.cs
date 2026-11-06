using Kitch.Application.DTOs.StockUsuarios;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class StockUsuarioService : IStockUsuarioService
{
    private readonly IRepository<StockUsuario> _repository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IIngredienteNormalizerService _normalizer;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public StockUsuarioService(
        IRepository<StockUsuario> repository,
        IRepository<Ingrediente> ingredienteRepository,
        IIngredienteNormalizerService normalizer,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
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

    public Task<StockUsuarioResponseDto> CreateAsync(StockUsuarioCreateDto stock) =>
        EnTransaccionAsync(stock.UsuarioId, () => CreateEnTransaccionAsync(stock));

    private Task<T> EnTransaccionAsync<T>(int usuarioId, Func<Task<T>> operacion) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await _unidadDeTrabajo.BloquearRecursoAsync($"kitch:stock:usuario:{usuarioId}");
            return await operacion();
        });

    private async Task<StockUsuarioResponseDto> CreateEnTransaccionAsync(StockUsuarioCreateDto stock)
    {
        ValidateCantidad(stock.Cantidad);

        var ingredienteId = await ResolverIngredienteIdAsync(stock.IngredienteId, stock.NombreIngrediente);

        var stockExistente = await _repository.FirstOrDefaultAsync(existente =>
            existente.UsuarioId == stock.UsuarioId &&
            existente.IngredienteId == ingredienteId);

        var unidadNueva = NormalizarUnidad(stock.UnidadMedida);

        if (stockExistente is not null)
        {
            if (stockExistente.Cantidad <= 0)
            {
                stockExistente.Cantidad = stock.Cantidad;
                stockExistente.UnidadMedida = unidadNueva;
            }
            else if (UnidadesMedida.TryConvertir(stock.Cantidad, unidadNueva, stockExistente.UnidadMedida, out var convertida))
            {
                stockExistente.Cantidad += convertida;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Ya tenés este ingrediente cargado en '{stockExistente.UnidadMedida}' y no se puede sumar en '{unidadNueva}'. " +
                    "Cargalo en la misma unidad o editá la cantidad existente.");
            }

            if (stock.FechaCaducidad.HasValue &&
                (stockExistente.FechaCaducidad is null || stock.FechaCaducidad < stockExistente.FechaCaducidad))
            {
                stockExistente.FechaCaducidad = stock.FechaCaducidad;
            }

            await _repository.UpdateAsync(stockExistente);
            return (await GetByIdAsync(stockExistente.Id, stock.UsuarioId))!;
        }

        var entity = new StockUsuario
        {
            UsuarioId = stock.UsuarioId,
            IngredienteId = ingredienteId,
            Cantidad = stock.Cantidad,
            UnidadMedida = unidadNueva,
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
            if (nombre.Length > 100)
            {
                nombre = nombre[..100].TrimEnd();
            }
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

    public Task<bool> UpdateAsync(int id, StockUsuarioUpdateDto stock, int usuarioId) =>
        EnTransaccionAsync(usuarioId, () => UpdateEnTransaccionAsync(id, stock, usuarioId));

    private async Task<bool> UpdateEnTransaccionAsync(int id, StockUsuarioUpdateDto stock, int usuarioId)
    {
        ValidateCantidad(stock.Cantidad);

        var existingStock = await _repository.GetByIdAsync(id);

        if (existingStock is null || existingStock.UsuarioId != usuarioId)
        {
            return false;
        }

        existingStock.Cantidad = stock.Cantidad;
        existingStock.UnidadMedida = NormalizarUnidad(stock.UnidadMedida);
        if (stock.FechaCaducidad.HasValue)
        {
            existingStock.FechaCaducidad = stock.FechaCaducidad;
        }

        await _repository.UpdateAsync(existingStock);

        return true;
    }

    public Task<bool> DeleteAsync(int id, int usuarioId) =>
        EnTransaccionAsync(usuarioId, () => DeleteEnTransaccionAsync(id, usuarioId));

    private async Task<bool> DeleteEnTransaccionAsync(int id, int usuarioId)
    {
        var stock = await _repository.GetByIdAsync(id);

        if (stock is null || stock.UsuarioId != usuarioId)
        {
            return false;
        }

        await _repository.DeleteAsync(stock);

        return true;
    }

    private static string NormalizarUnidad(string? unidad)
    {
        var limpia = unidad?.Trim() ?? string.Empty;
        if (limpia.Length == 0)
        {
            return UnidadesMedida.Unidad;
        }

        return limpia.Length > 50 ? limpia[..50] : limpia;
    }

    private static void ValidateCantidad(decimal cantidad)
    {
        if (cantidad < 0)
        {
            throw new InvalidOperationException("La cantidad disponible no puede ser negativa.");
        }
    }
}
