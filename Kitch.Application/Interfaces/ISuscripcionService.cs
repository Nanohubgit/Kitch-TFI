using Kitch.Application.DTOs.Suscripciones;

namespace Kitch.Application.Interfaces;

public interface ISuscripcionService
{
    Task<IEnumerable<SuscripcionResponseDto>> GetAllAsync();
    Task<SuscripcionResponseDto?> GetByIdAsync(int id);
    Task<SuscripcionResponseDto> CreateAsync(SuscripcionCreateDto suscripcion);
    Task<bool> UpdateAsync(int id, SuscripcionUpdateDto suscripcion);
    Task<bool> DeleteAsync(int id);

    Task<IniciarPagoResponseDto> ContratarAsync(int usuarioId, ContratarSuscripcionRequest? request);

    /// <summary>
    /// Confirma un pago notificado por la pasarela. Único lugar que puede promover a Profesional.
    /// </summary>
    Task ProcesarNotificacionPagoAsync(string paymentId);
}
