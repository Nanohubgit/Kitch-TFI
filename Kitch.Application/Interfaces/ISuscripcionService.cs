using Kitch.Application.DTOs.Suscripciones;

namespace Kitch.Application.Interfaces;

public interface ISuscripcionService
{
    Task<IEnumerable<SuscripcionResponseDto>> GetAllAsync();
    Task<SuscripcionResponseDto?> GetByIdAsync(int id);
    Task<SuscripcionResponseDto> CreateAsync(SuscripcionCreateDto suscripcion);
    Task<bool> UpdateAsync(int id, SuscripcionUpdateDto suscripcion);
    Task<bool> DeleteAsync(int id);

    Task<ContratarSuscripcionResult> ContratarAsync(int usuarioId, ContratarSuscripcionRequest request);
}
