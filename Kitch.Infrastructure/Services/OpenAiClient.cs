using Microsoft.Extensions.Configuration;

namespace Kitch.Infrastructure.Services;

public class OpenAiClient : ChatCompletionsClientBase
{
    public const string NombreCliente = "OpenAiClient";
    public const string SeccionConfiguracion = "OpenAi";

    public OpenAiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : base(
            httpClientFactory,
            NombreCliente,
            "v1/chat/completions",
            configuration.GetSection(SeccionConfiguracion))
    {
    }
}
