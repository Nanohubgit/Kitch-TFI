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

    /// <summary>
    /// Cascarón: prepara CheckoutUrl/PreferenceId para el modal de pago del front.
    /// </summary>
    Task<CheckoutSuscripcionResponseDto> IniciarCheckoutAsync(int usuarioId, CheckoutSuscripcionRequestDto request);

    /// <summary>
    /// Cascarón: confirma pago de la pasarela y asciende el rol a Profesional.
    /// </summary>
    Task<WebhookPagoResponseDto> ProcesarWebhookAsync(WebhookPagoRequestDto request);
}
