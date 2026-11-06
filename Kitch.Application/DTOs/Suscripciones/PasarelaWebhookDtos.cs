using System.Text.Json.Serialization;

namespace Kitch.Application.DTOs.Suscripciones;

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
