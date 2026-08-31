using Kitch.Application.DTOs.ContratosSub;

namespace Kitch.Application.Interfaces;

public interface IContratoSubService
{
    Task<IEnumerable<ContratoSubResponseDto>> GetAllAsync(int solicitanteId);
    Task<IEnumerable<ContratoSubResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<ContratoSubResponseDto?> GetByIdAsync(int id, int solicitanteId);
    Task<ContratoSubResponseDto> CreateAsync(ContratoSubCreateDto contratoSub, int solicitanteId);
    Task<bool> UpdateAsync(int id, ContratoSubUpdateDto contratoSub, int solicitanteId);
    Task<bool> DeleteAsync(int id, int solicitanteId);
}
