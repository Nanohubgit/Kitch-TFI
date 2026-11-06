using Kitch.Application.DTOs.Suscripciones;

namespace Kitch.Application.Interfaces;

public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> ProcesarPagoAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<CheckoutPreferenceResult> CrearPreferenciaAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentNotificationResult?> ConsultarPagoAsync(
        string paymentId,
        CancellationToken cancellationToken = default);

    bool FirmaWebhookValida(string? firma, string? requestId, string paymentId);
}
