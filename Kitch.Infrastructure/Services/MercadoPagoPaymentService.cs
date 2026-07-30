using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kitch.Infrastructure.Services;

/// <summary>
/// Pasarela MercadoPago (modo dummy). Simula latencia y aprueba el cobro
/// para cerrar el flujo end-to-end sin SDK ni Access Token reales.
/// </summary>
public class MercadoPagoPaymentService : IPaymentGatewayService
{
    private readonly IConfiguration _configuration;

    public MercadoPagoPaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<PaymentGatewayResult> ProcesarPagoAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        // TODO: Integrar SDK real de MP.
        // var accessToken = _configuration["MercadoPago:AccessToken"];
        // var publicKey = _configuration["MercadoPago:PublicKey"];
        // var client = new MercadoPagoClient(accessToken);
        // var payment = await client.CreatePaymentAsync(...);

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
            TransaccionId = $"mp_dummy_{Guid.NewGuid():N}",
            Mensaje = "Pago simulado aprobado (MercadoPago dummy)."
        };
    }
}
