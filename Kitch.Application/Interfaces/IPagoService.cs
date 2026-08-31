using Kitch.Application.DTOs.Pagos;

namespace Kitch.Application.Interfaces;

public interface IPagoService
{
    Task<IEnumerable<PagoResponseDto>> GetAllAsync(int solicitanteId);
    Task<IEnumerable<PagoResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<PagoResponseDto?> GetByIdAsync(int id, int solicitanteId);
    Task<PagoResponseDto> CreateAsync(PagoCreateDto pago, int solicitanteId);
    Task<bool> UpdateAsync(int id, PagoUpdateDto pago, int solicitanteId);
    Task<bool> DeleteAsync(int id, int solicitanteId);
}
