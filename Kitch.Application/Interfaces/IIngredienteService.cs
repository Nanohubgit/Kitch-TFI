using Kitch.Application.DTOs.Ingredientes;

namespace Kitch.Application.Interfaces;

public interface IIngredienteService
{
    Task<IEnumerable<IngredienteResponseDto>> GetAllAsync(int usuarioId);
    Task<IngredienteResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<HashSet<int>> ObtenerIdsAccesiblesAsync(int usuarioId);
    Task<IngredienteResponseDto> CreateAsync(IngredienteCreateDto ingrediente, int solicitanteId);
    Task<bool> UpdateAsync(int id, IngredienteUpdateDto ingrediente, int solicitanteId);
    Task<bool> DeleteAsync(int id, int solicitanteId);
}
