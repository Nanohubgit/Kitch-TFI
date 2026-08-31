using Kitch.Application.DTOs.Suscripciones;

namespace Kitch.Application.Interfaces;

public interface ISuscripcionService
{
    Task<IEnumerable<SuscripcionResponseDto>> GetAllAsync(int solicitanteId);
    Task<IEnumerable<SuscripcionResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<SuscripcionResponseDto?> GetByIdAsync(int id, int solicitanteId);
    Task<SuscripcionResponseDto> CreateAsync(SuscripcionCreateDto suscripcion, int solicitanteId);
    Task<bool> UpdateAsync(int id, SuscripcionUpdateDto suscripcion, int solicitanteId);
    Task<bool> DeleteAsync(int id, int solicitanteId);

    Task<IniciarPagoResponseDto> ContratarAsync(int usuarioId, ContratarSuscripcionRequest? request);

    /// <summary>
    /// Confirma un pago notificado por la pasarela. Único lugar que puede promover a Profesional.
    /// </summary>
    Task ProcesarNotificacionPagoAsync(string paymentId);
}
