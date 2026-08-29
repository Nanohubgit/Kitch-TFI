using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.Extensions.Configuration;

namespace Kitch.Infrastructure.Services;

/// <summary>
/// Checkout Pro de Mercado Pago. Crea preferencias y consulta pagos.
/// No cambia roles: eso lo hace Application al procesar el webhook.
/// </summary>
public class MercadoPagoPaymentService : IPaymentGatewayService
{
    private readonly IConfiguration _configuration;

    public MercadoPagoPaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
        AsegurarAccessToken();
    }

    public Task<PaymentGatewayResult> ProcesarPagoAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Checkout Pro no cobra en el servidor. Usá CrearPreferenciaAsync y confirmá el pago por webhook.");
    }

    public async Task<CheckoutPreferenceResult> CrearPreferenciaAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        AsegurarAccessToken();

        var successUrl = ValorODefault(_configuration["MercadoPago:FrontSuccessUrl"], "http://localhost:5173/pago-exitoso");
        var failureUrl = ValorODefault(_configuration["MercadoPago:FrontFailureUrl"], "http://localhost:5173/perfil");
        var notificationUrl = _configuration["MercadoPago:NotificationUrl"]?.Trim();

        var preferenceRequest = new PreferenceRequest
        {
            Items =
            [
                new PreferenceItemRequest
                {
                    Title = string.IsNullOrWhiteSpace(request.Descripcion)
                        ? "Suscripción Profesional — Alacena Virtual"
                        : request.Descripcion,
                    Quantity = 1,
                    CurrencyId = PrecioSuscripcion.MonedaArs,
                    UnitPrice = request.Monto > 0 ? request.Monto : PrecioSuscripcion.ProfesionalArs
                }
            ],
            Payer = string.IsNullOrWhiteSpace(request.EmailUsuario)
                ? null
                : new PreferencePayerRequest { Email = request.EmailUsuario },
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = successUrl,
                Failure = failureUrl,
                Pending = failureUrl
            },
            ExternalReference = request.UsuarioId.ToString(),
            StatementDescriptor = "KITCH PRO"
        };

        if (!string.IsNullOrWhiteSpace(notificationUrl))
        {
            preferenceRequest.NotificationUrl = notificationUrl;
        }

        if (successUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            preferenceRequest.AutoReturn = "approved";
        }

        var client = new PreferenceClient();
        Preference preference = await client.CreateAsync(preferenceRequest, cancellationToken: cancellationToken);

        var initPoint = string.IsNullOrWhiteSpace(preference.InitPoint)
            ? preference.SandboxInitPoint
            : preference.InitPoint;

        if (string.IsNullOrWhiteSpace(initPoint))
        {
            throw new InvalidOperationException("Mercado Pago no devolvió una URL de checkout (InitPoint).");
        }

        return new CheckoutPreferenceResult
        {
            InitPoint = initPoint,
            PreferenceId = preference.Id ?? string.Empty
        };
    }

    public async Task<PaymentNotificationResult?> ConsultarPagoAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        AsegurarAccessToken();

        if (!long.TryParse(paymentId, out var id))
        {
            return null;
        }

        var client = new PaymentClient();
        var payment = await client.GetAsync(id, cancellationToken: cancellationToken);

        if (payment is null)
        {
            return null;
        }

        var status = payment.Status ?? string.Empty;

        return new PaymentNotificationResult
        {
            Aprobado = status.Equals("approved", StringComparison.OrdinalIgnoreCase),
            Status = status,
            ExternalReference = payment.ExternalReference,
            TransaccionId = payment.Id?.ToString(),
            Monto = payment.TransactionAmount ?? 0
        };
    }

    private void AsegurarAccessToken()
    {
        var accessToken = _configuration["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException(
                "MercadoPago:AccessToken no está configurado. Definilo en User Secrets o en Azure.");
        }

        MercadoPagoConfig.AccessToken = accessToken;
    }

    private static string ValorODefault(string? valor, string fallback) =>
        string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
}
