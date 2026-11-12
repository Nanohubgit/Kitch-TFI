using System.Globalization;
using System.Net.Http.Headers;
using Kitch.Application.Common;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Interfaces;
using Kitch.Infrastructure.Repositories;
using Kitch.Infrastructure.Services;
using MercadoPago.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kitch.Infrastructure;

public static class DependencyInjection
{
    private const int TimeoutIaSegundosPorDefecto = 60;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton(CrearPlanSuscripcion(configuration));

        var mercadoPagoToken = configuration["MercadoPago:AccessToken"];
        if (!string.IsNullOrWhiteSpace(mercadoPagoToken))
        {
            MercadoPagoConfig.AccessToken = mercadoPagoToken;
        }

        services.AddScoped<IPaymentGatewayService, MercadoPagoPaymentService>();

        var proveedorIa = configuration["Ai:Provider"]?.Trim();
        var usarOpenAi = string.Equals(proveedorIa, "OpenAI", StringComparison.OrdinalIgnoreCase);
        if (usarOpenAi)
        {
            services.AddScoped<IAsistenteIaClient, OpenAiClient>();
        }
        else
        {
            services.AddScoped<IAsistenteIaClient, GroqClient>();
        }

        RegistrarClienteIa(services, GroqClient.NombreCliente, GroqClient.SeccionConfiguracion);
        RegistrarClienteIa(services, OpenAiClient.NombreCliente, OpenAiClient.SeccionConfiguracion);

        return services;
    }

    private static void RegistrarClienteIa(
        IServiceCollection services,
        string nombreCliente,
        string seccion)
    {
        services.AddHttpClient(nombreCliente, (serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var apiKey = config[$"{seccion}:ApiKey"]?.Trim();
            var baseUrl = config[$"{seccion}:BaseUrl"]?.Trim();
            var timeout = int.TryParse(config["Ai:TimeoutSegundos"], out var segundos) && segundos > 0
                ? segundos
                : TimeoutIaSegundosPorDefecto;

            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(AsegurarBarraFinal(baseUrl));
            }

            client.Timeout = TimeSpan.FromSeconds(timeout);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        });
    }

    private static PlanSuscripcionConfig CrearPlanSuscripcion(IConfiguration configuration)
    {
        var precio = decimal.TryParse(
            configuration["Suscripciones:PrecioProfesionalArs"],
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var configurado) && configurado > 0
            ? configurado
            : PrecioSuscripcion.ProfesionalArs;

        var moneda = configuration["Suscripciones:Moneda"]?.Trim();

        return new PlanSuscripcionConfig
        {
            PrecioProfesionalArs = precio,
            Moneda = string.IsNullOrWhiteSpace(moneda) ? PrecioSuscripcion.MonedaArs : moneda
        };
    }

    private static string AsegurarBarraFinal(string url) => url.EndsWith('/') ? url : url + "/";
}
