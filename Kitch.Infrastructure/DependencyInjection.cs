using Kitch.Application.Interfaces;
using Kitch.Domain.Interfaces;
using Kitch.Infrastructure.Repositories;
using Kitch.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kitch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // AuthService se queda en Infrastructure porque maneja JWT (detalle de infraestructura)
        services.AddScoped<IAuthService, AuthService>();

        // CRUD del catálogo de sustitutos (su implementación vive en Infrastructure).
        services.AddScoped<ISustitutoService, SustitutoService>();

        services.AddScoped<IGeminiClient, GeminiClient>();

        services.AddHttpClient("GeminiClient", (serviceProvider, client) =>
        {
            // Buscamos la configuración que Nano ya guardó en los User Secrets
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var apiKey = configuration["Gemini:ApiKey"];

            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            // Inyectamos la API Key de Google de forma segura en los headers correspondientes o preparamos la URL base
            client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
        });

        return services;
    }
}
