using Kitch.Application.DTOs.Alacena;
using Kitch.Application.DTOs.StockUsuarios;
using Kitch.Application.Interfaces;

namespace Kitch.Application.Services;

/// <summary>
/// Fachada de Alacena sobre el inventario existente (StockUsuario).
/// No duplica reglas: reutiliza IStockUsuarioService.
/// </summary>
public class AlacenaService : IAlacenaService
{
    private readonly IStockUsuarioService _stockUsuarioService;

    public AlacenaService(IStockUsuarioService stockUsuarioService)
    {
        _stockUsuarioService = stockUsuarioService;
    }

    public async Task<IEnumerable<IngredienteAlacenaResponseDto>> ObtenerInventarioAsync(int usuarioId)
    {
        var stock = await _stockUsuarioService.GetByUsuarioIdAsync(usuarioId);

        return stock
            .Select(Map)
            .OrderBy(item => item.NombreIngrediente, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IngredienteAlacenaResponseDto?> ObtenerPorIdAsync(int id, int usuarioId)
    {
        var stock = await _stockUsuarioService.GetByIdAsync(id, usuarioId);
        return stock is null ? null : Map(stock);
    }

    public async Task<IngredienteAlacenaResponseDto> AgregarIngredienteAsync(
        int usuarioId,
        AgregarIngredienteRequestDto request)
    {
        var creado = await _stockUsuarioService.CreateAsync(new StockUsuarioCreateDto
        {
            UsuarioId = usuarioId,
            IngredienteId = request.IngredienteId,
            NombreIngrediente = request.NombreIngrediente,
            Cantidad = request.Cantidad,
            UnidadMedida = request.UnidadMedida,
            FechaCaducidad = request.FechaCaducidad
        });

        var completo = await _stockUsuarioService.GetByIdAsync(creado.Id, usuarioId);
        return Map(completo ?? creado);
    }

    public async Task<IngredienteAlacenaResponseDto?> ActualizarCantidadAsync(
        int id,
        int usuarioId,
        ActualizarCantidadAlacenaRequestDto request)
    {
        var existente = await _stockUsuarioService.GetByIdAsync(id, usuarioId);
        if (existente is null)
        {
            return null;
        }

        var actualizado = await _stockUsuarioService.UpdateAsync(id, new StockUsuarioUpdateDto
        {
            Cantidad = request.Cantidad,
            UnidadMedida = string.IsNullOrWhiteSpace(request.UnidadMedida)
                ? existente.UnidadMedida
                : request.UnidadMedida.Trim()
        }, usuarioId);

        if (!actualizado)
        {
            return null;
        }

        var completo = await _stockUsuarioService.GetByIdAsync(id, usuarioId);
        return completo is null ? null : Map(completo);
    }

    public Task<bool> EliminarIngredienteAsync(int id, int usuarioId) =>
        _stockUsuarioService.DeleteAsync(id, usuarioId);

    private static IngredienteAlacenaResponseDto Map(StockUsuarioResponseDto stock) => new()
    {
        Id = stock.Id,
        IngredienteId = stock.IngredienteId,
        NombreIngrediente = stock.NombreIngrediente,
        Cantidad = stock.Cantidad,
        UnidadMedida = stock.UnidadMedida,
        FechaCaducidad = stock.FechaCaducidad
    };
}
