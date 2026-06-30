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

        // Cliente de IA: usamos Groq (free tier más generosa, API compatible con OpenAI).
        // Se inyecta detrás de IAsistenteIaClient para no acoplar la aplicación al proveedor.
        services.AddScoped<IAsistenteIaClient, GroqClient>();

        services.AddHttpClient("GroqClient", (serviceProvider, client) =>
        {
            // La API Key de Groq la leemos de la configuración (User Secrets / appsettings).
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var apiKey = configuration["Groq:ApiKey"];

            client.BaseAddress = new Uri("https://api.groq.com/");
            // Groq usa autenticación Bearer estándar de OpenAI.
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        });

        return services;
    }
}
