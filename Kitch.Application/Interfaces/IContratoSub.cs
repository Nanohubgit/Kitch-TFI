using Kitch.Application.DTOs.ContratosSub;

namespace Kitch.Application.Interfaces;

public interface IContratoSubService
{
    Task<IEnumerable<ContratoSubResponseDto>> GetAllAsync();
    Task<IEnumerable<ContratoSubResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<ContratoSubResponseDto?> GetByIdAsync(int id);
    Task<ContratoSubResponseDto> CreateAsync(ContratoSubCreateDto contratoSub);
    Task<bool> UpdateAsync(int id, ContratoSubUpdateDto contratoSub);
    Task<bool> DeleteAsync(int id);
}
