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
}
