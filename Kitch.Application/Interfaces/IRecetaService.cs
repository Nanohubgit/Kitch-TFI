using Kitch.Application.DTOs.Recetas;

namespace Kitch.Application.Interfaces;

public interface IRecetaService
{
    Task<IEnumerable<RecetaResponseDto>> GetAllAsync(int usuarioId);
    Task<RecetaResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<RecetaResponseDto> CreateAsync(RecetaCreateDto receta, int usuarioId);
    Task<bool> UpdateAsync(int id, RecetaUpdateDto receta, int usuarioId);
    Task<bool> DeleteAsync(int id, int solicitanteId);
    Task<IEnumerable<RecetaResponseDto>> GetByDificultadAsync(string dificultad, int usuarioId);
    Task<IEnumerable<RecetaResponseDto>> GetByCategoriaAsync(string categoria, int usuarioId);
    IReadOnlyList<string> GetCategorias();
}
