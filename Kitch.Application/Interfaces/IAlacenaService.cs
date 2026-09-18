using Kitch.Application.DTOs.Alacena;

namespace Kitch.Application.Interfaces;

public interface IAlacenaService
{
    Task<IEnumerable<IngredienteAlacenaResponseDto>> ObtenerInventarioAsync(int usuarioId);
    Task<IngredienteAlacenaResponseDto?> ObtenerPorIdAsync(int id, int usuarioId);
    Task<IngredienteAlacenaResponseDto> AgregarIngredienteAsync(int usuarioId, AgregarIngredienteRequestDto request);
    Task<IngredienteAlacenaResponseDto?> ActualizarCantidadAsync(int id, int usuarioId, ActualizarCantidadAlacenaRequestDto request);
    Task<bool> EliminarIngredienteAsync(int id, int usuarioId);
}
