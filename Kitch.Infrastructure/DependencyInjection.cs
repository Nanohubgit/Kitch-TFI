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
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Pasarelas concretas (dummy hoy; mañana SDKs reales).
        services.AddScoped<StripePaymentService>();
        services.AddScoped<MercadoPagoPaymentService>();
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        // Application solo conoce IPaymentGatewayService: el factory elige
        // Stripe o MercadoPago según appsettings "PaymentGateway".
        services.AddScoped<IPaymentGatewayService>(sp =>
            sp.GetRequiredService<IPaymentGatewayFactory>().Create());

        services.AddScoped<IAsistenteIaClient, GroqClient>();

        services.AddHttpClient("GroqClient", (serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var apiKey = configuration["Groq:ApiKey"];

            client.BaseAddress = new Uri("https://api.groq.com/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        });

        return services;
    }
}
