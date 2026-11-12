using Microsoft.Extensions.Configuration;

namespace Kitch.Infrastructure.Services;

public class GroqClient : ChatCompletionsClientBase
{
    public const string NombreCliente = "GroqClient";
    public const string SeccionConfiguracion = "Groq";

    public GroqClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : base(
            httpClientFactory,
            NombreCliente,
            "openai/v1/chat/completions",
            configuration.GetSection(SeccionConfiguracion))
    {
    }
}
