using Kitch.Application.DTOs.Sustitutos;

namespace Kitch.Application.Interfaces;

public interface ISustitutoService
{
    Task<IEnumerable<SustitutoResponseDto>> GetAllAsync();
    Task<IEnumerable<SustitutoResponseDto>> GetByIngredienteIdAsync(int ingredienteId, int usuarioId);
    Task<SustitutoResponseDto?> GetByIdAsync(int id);
    Task<SustitutoResponseDto> CreateAsync(SustitutoCreateDto sustituto);
    Task<bool> UpdateAsync(int id, SustitutoUpdateDto sustituto);
    Task<bool> DeleteAsync(int id);
}
