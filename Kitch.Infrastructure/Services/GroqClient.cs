using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kitch.Infrastructure.Services;

public class GroqClient : IAsistenteIaClient
{
    private const string ChatEndpoint = "openai/v1/chat/completions";
    private const string ModeloPorDefecto = "openai/gpt-oss-120b";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _modelo;

    public GroqClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;

        var modeloConfig = configuration["Groq:Model"];
        _modelo = string.IsNullOrWhiteSpace(modeloConfig) ? ModeloPorDefecto : modeloConfig;
    }

    public Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction) =>
        EnviarAsync(
            [new GroqMessage { Role = "user", Content = prompt }],
            systemInstruction,
            jsonMode: false);

    public Task<string> GenerarRespuestaJsonAsync(string prompt, string systemInstruction) =>
        EnviarAsync(
            [new GroqMessage { Role = "user", Content = prompt }],
            systemInstruction,
            jsonMode: true);

    public Task<string> GenerarRespuestaConversacionAsync(
        IEnumerable<MensajeIa> mensajes,
        string systemInstruction,
        bool jsonMode = false)
    {
        var messages = mensajes
            .Select(mensaje => new GroqMessage
            {
                Role = string.Equals(mensaje.Rol, "model", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(mensaje.Rol, "assistant", StringComparison.OrdinalIgnoreCase)
                    ? "assistant"
                    : "user",
                Content = mensaje.Texto
            })
            .ToList();

        return EnviarAsync(messages, systemInstruction, jsonMode);
    }

    private async Task<string> EnviarAsync(List<GroqMessage> messages, string systemInstruction, bool jsonMode)
    {
        var client = _httpClientFactory.CreateClient("GroqClient");

        var todosLosMensajes = new List<GroqMessage>
        {
            new() { Role = "system", Content = systemInstruction }
        };
        todosLosMensajes.AddRange(messages);

        var request = new GroqRequest
        {
            Model = _modelo,
            Messages = todosLosMensajes,
            ResponseFormat = jsonMode
                ? new GroqResponseFormat { Type = "json_object" }
                : null
        };

        const int maxIntentos = 3;

        for (var intento = 1; ; intento++)
        {
            using var response = await client.PostAsJsonAsync(ChatEndpoint, request);

            if (response.IsSuccessStatusCode)
            {
                var resultado = await response.Content.ReadFromJsonAsync<GroqResponse>();

                var textoLimpio = resultado?
                    .Choices?
                    .FirstOrDefault()?
                    .Message?
                    .Content;

                return textoLimpio ?? string.Empty;
            }

            var esTransitorio = response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == HttpStatusCode.TooManyRequests;

            if (esTransitorio && intento < maxIntentos)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(700 * intento));
                continue;
            }

            response.EnsureSuccessStatusCode();
        }
    }

    private sealed class GroqRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<GroqMessage> Messages { get; set; } = [];

        [JsonPropertyName("response_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroqResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class GroqResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    private sealed class GroqMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class GroqResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChoice>? Choices { get; set; }
    }

    private sealed class GroqChoice
    {
        [JsonPropertyName("message")]
        public GroqMessage? Message { get; set; }
    }
}
