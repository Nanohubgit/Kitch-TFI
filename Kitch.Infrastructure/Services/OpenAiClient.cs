using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kitch.Infrastructure.Services;

/// <summary>
/// Cliente OpenAI (gpt-4o-mini por defecto) vía HttpClientFactory.
/// Se activa con Ai:Provider = OpenAI. El modelo se lee de OpenAi:Model.
/// </summary>
public class OpenAiClient : IAsistenteIaClient
{
    private const string ChatEndpoint = "v1/chat/completions";
    private const string ModeloPorDefecto = "gpt-4o-mini";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _modelo;

    public OpenAiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;

        var modeloConfig = configuration["OpenAi:Model"];
        _modelo = string.IsNullOrWhiteSpace(modeloConfig) ? ModeloPorDefecto : modeloConfig.Trim();
    }

    public Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction) =>
        EnviarAsync(
            [new OpenAiMessage { Role = "user", Content = prompt }],
            systemInstruction,
            jsonMode: false);

    public Task<string> GenerarRespuestaJsonAsync(string prompt, string systemInstruction) =>
        EnviarAsync(
            [new OpenAiMessage { Role = "user", Content = prompt }],
            systemInstruction,
            jsonMode: true);

    public Task<string> GenerarRespuestaConversacionAsync(
        IEnumerable<MensajeIa> mensajes,
        string systemInstruction,
        bool jsonMode = false)
    {
        var messages = mensajes
            .Select(mensaje => new OpenAiMessage
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

    private async Task<string> EnviarAsync(List<OpenAiMessage> messages, string systemInstruction, bool jsonMode)
    {
        var client = _httpClientFactory.CreateClient("OpenAiClient");

        var todosLosMensajes = new List<OpenAiMessage>
        {
            new() { Role = "system", Content = systemInstruction }
        };
        todosLosMensajes.AddRange(messages);

        var request = new OpenAiRequest
        {
            Model = _modelo,
            Messages = todosLosMensajes,
            ResponseFormat = jsonMode
                ? new OpenAiResponseFormat { Type = "json_object" }
                : null
        };

        const int maxIntentos = 3;

        for (var intento = 1; ; intento++)
        {
            using var response = await client.PostAsJsonAsync(ChatEndpoint, request);

            if (response.IsSuccessStatusCode)
            {
                var resultado = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
                return resultado?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
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

    private sealed class OpenAiRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAiMessage> Messages { get; set; } = [];

        [JsonPropertyName("response_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenAiResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class OpenAiResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OpenAiResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }
}
