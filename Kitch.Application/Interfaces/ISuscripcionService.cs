using Kitch.Application.DTOs.Suscripciones;

namespace Kitch.Application.Interfaces;

public interface ISuscripcionService
{
    Task<IEnumerable<SuscripcionResponseDto>> GetAllAsync();
    Task<SuscripcionResponseDto?> GetByIdAsync(int id);
    Task<SuscripcionResponseDto> CreateAsync(SuscripcionCreateDto suscripcion);
    Task<bool> UpdateAsync(int id, SuscripcionUpdateDto suscripcion);
    Task<bool> DeleteAsync(int id);

    // Orquesta el alta de la suscripción: contrato + pago simulado y, si se aprueba,
    // eleva el rol del usuario a "Profesional".
    Task<ContratarSuscripcionResult> ContratarAsync(int usuarioId, ContratarSuscripcionRequest request);
}
