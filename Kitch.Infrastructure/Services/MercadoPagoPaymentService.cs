using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using MercadoPago.Client;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Error;
using MercadoPago.Resource.Preference;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Kitch.Infrastructure.Services;

public class MercadoPagoPaymentService : IPaymentGatewayService
{
    private const string MensajeNoDisponible =
        "Mercado Pago no está disponible en este momento. Intentá nuevamente en unos minutos.";

    private readonly IConfiguration _configuration;
    private readonly ILogger<MercadoPagoPaymentService> _logger;

    public MercadoPagoPaymentService(IConfiguration configuration, ILogger<MercadoPagoPaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
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
        var opciones = CrearOpciones();

        var successUrl = ValorODefault(_configuration["MercadoPago:FrontSuccessUrl"], "http://localhost:5173/pago-exitoso");
        var failureUrl = ValorODefault(_configuration["MercadoPago:FrontFailureUrl"], successUrl);
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
                    CurrencyId = string.IsNullOrWhiteSpace(request.Moneda) ? PrecioSuscripcion.MonedaArs : request.Moneda,
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

        Preference preference;
        try
        {
            preference = await new PreferenceClient().CreateAsync(preferenceRequest, opciones, cancellationToken);
        }
        catch (MercadoPagoException ex)
        {
            _logger.LogError(ex, "Mercado Pago rechazó la creación de la preferencia para el usuario {UsuarioId}", request.UsuarioId);
            throw new ServicioExternoException(MensajeNoDisponible, ex);
        }

        var initPoint = string.IsNullOrWhiteSpace(preference.InitPoint)
            ? preference.SandboxInitPoint
            : preference.InitPoint;

        if (string.IsNullOrWhiteSpace(initPoint))
        {
            throw new ServicioExternoException("Mercado Pago no devolvió una URL de checkout. Intentá nuevamente.");
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
        if (!long.TryParse(paymentId?.Trim(), out var id) || id <= 0)
        {
            return null;
        }

        var opciones = CrearOpciones();

        MercadoPago.Resource.Payment.Payment? payment;
        try
        {
            payment = await new PaymentClient().GetAsync(id, opciones, cancellationToken);
        }
        catch (MercadoPagoApiException ex) when (ex.ApiResponse?.StatusCode is 404)
        {
            return null;
        }
        catch (MercadoPagoException ex)
        {
            _logger.LogError(ex, "No se pudo consultar el pago {PaymentId} en Mercado Pago", paymentId);
            throw new ServicioExternoException(MensajeNoDisponible, ex);
        }

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
            Monto = payment.TransactionAmount ?? 0,
            MetodoPago = MapearMetodoPago(payment.PaymentTypeId)
        };
    }

    private static MetodoPago MapearMetodoPago(string? tipo) => tipo?.Trim().ToLowerInvariant() switch
    {
        "credit_card" => MetodoPago.TarjetaCredito,
        "debit_card" or "prepaid_card" => MetodoPago.TarjetaDebito,
        "account_money" or "digital_wallet" or "digital_currency" => MetodoPago.BilleteraVirtual,
        "bank_transfer" => MetodoPago.TransferenciaBancaria,
        _ => MetodoPago.NoEspecificado
    };

    public bool FirmaWebhookValida(string? firma, string? requestId, string paymentId)
    {
        var secret = _configuration["MercadoPago:WebhookSecret"]?.Trim();
        if (string.IsNullOrWhiteSpace(secret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(firma) || string.IsNullOrWhiteSpace(paymentId))
        {
            return false;
        }

        var partes = firma.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? ts = null;
        string? hash = null;
        foreach (var parte in partes)
        {
            var kv = parte.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            if (kv[0].Equals("ts", StringComparison.OrdinalIgnoreCase))
            {
                ts = kv[1];
            }
            else if (kv[0].Equals("v1", StringComparison.OrdinalIgnoreCase))
            {
                hash = kv[1];
            }
        }

        if (string.IsNullOrWhiteSpace(ts) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        var manifiesto = $"id:{paymentId.ToLowerInvariant()};request-id:{requestId ?? string.Empty};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var calculado = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifiesto))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(calculado),
            Encoding.UTF8.GetBytes(hash.ToLowerInvariant()));
    }

    private RequestOptions CrearOpciones()
    {
        var accessToken = _configuration["MercadoPago:AccessToken"]?.Trim();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogError("Falta configurar MercadoPago:AccessToken");
            throw new ServicioExternoException("Los pagos no están disponibles por el momento. Contactá a un administrador.");
        }

        return new RequestOptions { AccessToken = accessToken };
    }

    private static string ValorODefault(string? valor, string fallback) =>
        string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
}
