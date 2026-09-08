using Kitch.Application.DTOs.Recetas;
using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IRecetaService
{
    Task<IEnumerable<RecetaResponseDto>> GetAllAsync(int usuarioId);
    Task<RecetaResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<RecetaResponseDto> CreateAsync(RecetaCreateDto receta, int usuarioId);
    Task<bool> UpdateAsync(int id, RecetaUpdateDto receta, int usuarioId);
    Task<bool> DeleteAsync(int id, int solicitanteId);
    Task<IEnumerable<RecetaResponseDto>> GetByDificultadAsync(DificultadReceta dificultad, int usuarioId);
}
