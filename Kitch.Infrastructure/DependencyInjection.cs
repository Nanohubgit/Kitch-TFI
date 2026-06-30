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

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<ISustitutoService, SustitutoService>();

        services.AddScoped<IGeminiClient, GeminiClient>();

        services.AddHttpClient("GeminiClient", (serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var apiKey = configuration["Gemini:ApiKey"];

            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
        });

        return services;
    }
}
