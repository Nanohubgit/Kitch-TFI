using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kitch.Infrastructure.Services;

/// <summary>
/// Pasarela Stripe (modo dummy). Simula latencia y aprueba el cobro
/// para cerrar el flujo end-to-end sin SDK ni API keys reales.
/// </summary>
public class StripePaymentService : IPaymentGatewayService
{
    private readonly IConfiguration _configuration;

    public StripePaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<PaymentGatewayResult> ProcesarPagoAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        // TODO: Integrar SDK real de Stripe.
        // var apiKey = _configuration["Stripe:ApiKey"];
        // var webhookSecret = _configuration["Stripe:WebhookSecret"];
        // StripeConfiguration.ApiKey = apiKey;
        // var intent = await paymentIntentService.CreateAsync(...);

        _ = _configuration;

        if (request.Monto <= 0)
        {
            return new PaymentGatewayResult
            {
                Aprobado = false,
                Mensaje = "El monto del pago es inválido."
            };
        }

        await Task.Delay(1000, cancellationToken);

        return new PaymentGatewayResult
        {
            Aprobado = true,
            TransaccionId = $"stripe_dummy_{Guid.NewGuid():N}",
            Mensaje = "Pago simulado aprobado (Stripe dummy)."
        };
    }

    public Task<CheckoutPreferenceResult> CrearPreferenciaAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Stripe no implementa Checkout Pro. Configurá PaymentGateway=MercadoPago.");
    }

    public Task<PaymentNotificationResult?> ConsultarPagoAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PaymentNotificationResult?>(null);
    }
}
