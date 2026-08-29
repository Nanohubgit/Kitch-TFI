using Kitch.Application.Interfaces;
using Kitch.Domain.Interfaces;
using Kitch.Infrastructure.Repositories;
using Kitch.Infrastructure.Services;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kitch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        var mercadoPagoToken = configuration["MercadoPago:AccessToken"];
        if (!string.IsNullOrWhiteSpace(mercadoPagoToken))
        {
            MercadoPagoConfig.AccessToken = mercadoPagoToken;
        }

        services.AddScoped<StripePaymentService>();
        services.AddScoped<MercadoPagoPaymentService>();
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        services.AddScoped<IPaymentGatewayService>(sp =>
            sp.GetRequiredService<IPaymentGatewayFactory>().Create());

        services.AddScoped<IAsistenteIaClient, GroqClient>();

        services.AddHttpClient("GroqClient", (serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var apiKey = config["Groq:ApiKey"];

            client.BaseAddress = new Uri("https://api.groq.com/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        });

        return services;
    }
}
