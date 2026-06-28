using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class GeminiClient : IGeminiClient
{
    private const string ModeloEndpoint = "v1beta/models/gemini-1.5-flash:generateContent";

    private readonly IHttpClientFactory _httpClientFactory;

    public GeminiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction)
    {
        var client = _httpClientFactory.CreateClient("GeminiClient");

        // Armamos el body con el formato nativo que exige la API de Google:
        // system_instruction para el contexto base y contents para el mensaje del usuario.
        var request = new GeminiRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = systemInstruction }]
            },
            Contents =
            [
                new GeminiContent
                {
                    Role = "user",
                    Parts = [new GeminiPart { Text = prompt }]
                }
            ]
        };

        using var response = await client.PostAsJsonAsync(ModeloEndpoint, request);
        response.EnsureSuccessStatusCode();

        var resultado = await response.Content.ReadFromJsonAsync<GeminiResponse>();

        // Extraemos el texto limpio de la primera candidata devuelta por el modelo.
        var textoLimpio = resultado?
            .Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text;

        return textoLimpio ?? string.Empty;
    }

    private sealed class GeminiRequest
    {
        [JsonPropertyName("system_instruction")]
        public GeminiContent? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
