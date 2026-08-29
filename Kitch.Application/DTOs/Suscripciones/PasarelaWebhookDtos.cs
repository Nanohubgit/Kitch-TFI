using System.Text.Json.Serialization;

namespace Kitch.Application.DTOs.Suscripciones;

/// <summary>
/// Notificación IPN/Webhook de la pasarela. Shape de Mercado Pago, sin tipos del SDK.
/// </summary>
public class PasarelaWebhookRequest
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("data")]
    public PasarelaWebhookData? Data { get; set; }
}

public class PasarelaWebhookData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
