using Kitch.Application.DTOs.Recetas;
using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IRecetaService
{
    Task<IEnumerable<RecetaResponseDto>> GetAllAsync();
    Task<RecetaResponseDto?> GetByIdAsync(int id);
    Task<RecetaResponseDto> CreateAsync(RecetaCreateDto receta);
    Task<bool> UpdateAsync(int id, RecetaUpdateDto receta);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<RecetaResponseDto>> GetByDificultadAsync(DificultadReceta dificultad);
}
