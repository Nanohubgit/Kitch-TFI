using Kitch.Application.DTOs.Suscripciones;

namespace Kitch.Application.Interfaces;

/// <summary>
/// Puerto de Application para cobros. La implementación concreta (Stripe, MercadoPago, etc.)
/// vive en Infrastructure; Application solo conoce este contrato.
/// </summary>
public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> ProcesarPagoAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea una preferencia de Checkout Pro y devuelve la URL de pago (InitPoint).
    /// </summary>
    Task<CheckoutPreferenceResult> CrearPreferenciaAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta un pago en la pasarela por su id de transacción.
    /// </summary>
    Task<PaymentNotificationResult?> ConsultarPagoAsync(
        string paymentId,
        CancellationToken cancellationToken = default);
}
