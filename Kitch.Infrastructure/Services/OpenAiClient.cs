using System.ClientModel;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace Kitch.Infrastructure.Services;

/// <summary>
/// Adaptador de Infrastructure: OpenAI gpt-4o-mini detrás de <see cref="IAsistenteIaClient"/>.
/// Application no conoce este tipo.
/// </summary>
public class OpenAiClient : IAsistenteIaClient
{
    private const string Modelo = "gpt-4o-mini";

    private readonly ChatClient _chatClient;

    public OpenAiClient(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAi:ApiKey no está configurada. Definila en User Secrets (local) o en la configuración de Azure.");
        }

        _chatClient = new ChatClient(model: Modelo, apiKey: apiKey);
    }

    public Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction) =>
        CompletarAsync(
            [
                new SystemChatMessage(systemInstruction),
                new UserChatMessage(prompt)
            ]);

    public Task<string> GenerarRespuestaJsonAsync(string prompt, string systemInstruction) =>
        CompletarAsync(
            [
                new SystemChatMessage(systemInstruction),
                new UserChatMessage(prompt)
            ]);

    public Task<string> GenerarRespuestaConversacionAsync(
        IEnumerable<MensajeIa> mensajes,
        string systemInstruction,
        bool jsonMode = false)
    {
        _ = jsonMode;
        var messages = MapearMensajes(mensajes, systemInstruction);
        return CompletarAsync(messages);
    }

    private static List<ChatMessage> MapearMensajes(IEnumerable<MensajeIa> mensajes, string systemInstruction)
    {
        var lista = new List<ChatMessage>
        {
            new SystemChatMessage(systemInstruction)
        };

        foreach (var mensaje in mensajes)
        {
            if (string.Equals(mensaje.Rol, "model", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mensaje.Rol, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                lista.Add(new AssistantChatMessage(mensaje.Texto));
            }
            else
            {
                lista.Add(new UserChatMessage(mensaje.Texto));
            }
        }

        return lista;
    }

    private async Task<string> CompletarAsync(List<ChatMessage> messages)
    {
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        const int maxIntentos = 3;

        for (var intento = 1; ; intento++)
        {
            try
            {
                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);
                return completion.Content.Count > 0
                    ? completion.Content[0].Text ?? string.Empty
                    : string.Empty;
            }
            catch (ClientResultException ex) when (
                intento < maxIntentos &&
                (ex.Status == 429 || ex.Status == 503))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(700 * intento));
            }
        }
    }
}
