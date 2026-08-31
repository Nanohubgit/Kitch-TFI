using Kitch.Application.DTOs.Recetas;
using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IRecetaService
{
    Task<IEnumerable<RecetaResponseDto>> GetAllAsync(string? rolUsuario = null);
    Task<RecetaResponseDto?> GetByIdAsync(int id, string? rolUsuario = null);
    Task<RecetaResponseDto> CreateAsync(RecetaCreateDto receta);
    Task<bool> UpdateAsync(int id, RecetaUpdateDto receta);
    Task<bool> DeleteAsync(int id, int solicitanteId);
    Task<IEnumerable<RecetaResponseDto>> GetByDificultadAsync(DificultadReceta dificultad, string? rolUsuario = null);
}
