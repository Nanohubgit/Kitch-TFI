using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kitch.Infrastructure.Services;

public abstract class ChatCompletionsClientBase : IAsistenteIaClient
{
    private const int MaxIntentos = 2;
    private const string MensajeSaturado = "El asistente de IA está recibiendo demasiadas consultas en este momento.";
    private const string MensajeNoDisponible = "El asistente de IA no está disponible en este momento.";
    private const string MensajeTimeout = "El asistente de IA tardó demasiado en responder.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _nombreCliente;
    private readonly string _endpoint;
    private readonly string _modelo;
    private readonly bool _configurado;

    protected ChatCompletionsClientBase(
        IHttpClientFactory httpClientFactory,
        string nombreCliente,
        string endpoint,
        IConfigurationSection seccion)
    {
        _httpClientFactory = httpClientFactory;
        _nombreCliente = nombreCliente;
        _endpoint = endpoint;
        _modelo = seccion["Model"]?.Trim() ?? string.Empty;
        _configurado =
            !string.IsNullOrWhiteSpace(seccion["ApiKey"]) &&
            !string.IsNullOrWhiteSpace(seccion["BaseUrl"]) &&
            !string.IsNullOrWhiteSpace(_modelo);
    }

    public Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction) =>
        EnviarAsync([new ChatMessage { Role = "user", Content = prompt }], systemInstruction, jsonMode: false);

    public Task<string> GenerarRespuestaJsonAsync(string prompt, string systemInstruction) =>
        EnviarAsync([new ChatMessage { Role = "user", Content = prompt }], systemInstruction, jsonMode: true);

    public Task<string> GenerarRespuestaConversacionAsync(
        IEnumerable<MensajeIa> mensajes,
        string systemInstruction,
        bool jsonMode = false)
    {
        var messages = mensajes
            .Select(mensaje => new ChatMessage
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

    private async Task<string> EnviarAsync(List<ChatMessage> messages, string systemInstruction, bool jsonMode)
    {
        if (!_configurado)
        {
            throw new ServicioIaException(MotivoFallaIa.NoDisponible, "El asistente de IA no está configurado en el servidor.");
        }

        var client = _httpClientFactory.CreateClient(_nombreCliente);

        var todosLosMensajes = new List<ChatMessage> { new() { Role = "system", Content = systemInstruction } };
        todosLosMensajes.AddRange(messages);

        var request = new ChatRequest
        {
            Model = _modelo,
            Messages = todosLosMensajes,
            ResponseFormat = jsonMode ? new ChatResponseFormat { Type = "json_object" } : null
        };

        for (var intento = 1; ; intento++)
        {
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsJsonAsync(_endpoint, request);
            }
            catch (TaskCanceledException ex)
            {
                throw new ServicioIaException(MotivoFallaIa.Timeout, MensajeTimeout, ex);
            }
            catch (HttpRequestException ex)
            {
                throw new ServicioIaException(MotivoFallaIa.NoDisponible, MensajeNoDisponible, ex);
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var resultado = await response.Content.ReadFromJsonAsync<ChatResponse>();
                        return resultado?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
                    }
                    catch (JsonException ex)
                    {
                        throw new ServicioIaException(MotivoFallaIa.NoDisponible, "El asistente de IA devolvió una respuesta inválida.", ex);
                    }
                }

                var saturado = response.StatusCode == HttpStatusCode.TooManyRequests;
                var esTransitorio = saturado || (int)response.StatusCode >= 500;

                if (esTransitorio && intento < MaxIntentos)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(700 * intento));
                    continue;
                }

                throw saturado
                    ? new ServicioIaException(MotivoFallaIa.Saturado, MensajeSaturado)
                    : new ServicioIaException(MotivoFallaIa.NoDisponible, MensajeNoDisponible);
            }
        }
    }

    private sealed class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("response_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ChatResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class ChatResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }
}
