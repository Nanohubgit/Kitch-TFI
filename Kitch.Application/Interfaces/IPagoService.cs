using Kitch.Application.DTOs.Pagos;

namespace Kitch.Application.Interfaces;

public interface IPagoService
{
    Task<IEnumerable<PagoResponseDto>> GetAllAsync();
    Task<IEnumerable<PagoResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<PagoResponseDto?> GetByIdAsync(int id);
    Task<PagoResponseDto> CreateAsync(PagoCreateDto pago);
    Task<bool> UpdateAsync(int id, PagoUpdateDto pago);
    Task<bool> DeleteAsync(int id);
}
