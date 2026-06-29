using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class GeminiClient : IGeminiClient
{
    private const string ModeloEndpoint = "v1beta/models/gemini-2.5-flash-lite:generateContent";

    private readonly IHttpClientFactory _httpClientFactory;

    public GeminiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction) =>
        EnviarAsync(
            [new GeminiContent { Role = "user", Parts = [new GeminiPart { Text = prompt }] }],
            systemInstruction,
            jsonMode: false);

    public Task<string> GenerarRespuestaJsonAsync(string prompt, string systemInstruction) =>
        EnviarAsync(
            [new GeminiContent { Role = "user", Parts = [new GeminiPart { Text = prompt }] }],
            systemInstruction,
            jsonMode: true);

    public Task<string> GenerarRespuestaConversacionAsync(
        IEnumerable<MensajeIa> mensajes,
        string systemInstruction,
        bool jsonMode = false)
    {
        // Mapeamos cada turno del historial al formato nativo de Gemini.
        // La API solo acepta los roles "user" y "model".
        var contents = mensajes
            .Select(mensaje => new GeminiContent
            {
                Role = string.Equals(mensaje.Rol, "model", StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user",
                Parts = [new GeminiPart { Text = mensaje.Texto }]
            })
            .ToList();

        return EnviarAsync(contents, systemInstruction, jsonMode);
    }

    private async Task<string> EnviarAsync(List<GeminiContent> contents, string systemInstruction, bool jsonMode)
    {
        var client = _httpClientFactory.CreateClient("GeminiClient");

        // Armamos el body con el formato nativo que exige la API de Google:
        // system_instruction para el contexto base y contents para la conversación.
        var request = new GeminiRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = systemInstruction }]
            },
            Contents = contents,
            // En modo JSON le pedimos al modelo que devuelva únicamente JSON válido,
            // así lo podemos deserializar sin parsear texto libre.
            GenerationConfig = jsonMode
                ? new GeminiGenerationConfig { ResponseMimeType = "application/json" }
                : null
        };

        // Gemini puede responder 503 (modelo sobrecargado) o 429 (límite de uso) de forma
        // transitoria. Reintentamos unas pocas veces con espera creciente antes de fallar.
        const int maxIntentos = 3;

        for (var intento = 1; ; intento++)
        {
            using var response = await client.PostAsJsonAsync(ModeloEndpoint, request);

            if (response.IsSuccessStatusCode)
            {
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

            var esTransitorio = response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == HttpStatusCode.TooManyRequests;

            // Si todavía quedan intentos y el error es transitorio, esperamos y reintentamos.
            if (esTransitorio && intento < maxIntentos)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(700 * intento));
                continue;
            }

            // Sin más reintentos (o error no transitorio): lanzamos con el código de estado
            // para que la capa de aplicación devuelva un mensaje claro al usuario.
            response.EnsureSuccessStatusCode();
        }
    }

    private sealed class GeminiRequest
    {
        [JsonPropertyName("system_instruction")]
        public GeminiContent? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        public string ResponseMimeType { get; set; } = string.Empty;
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
