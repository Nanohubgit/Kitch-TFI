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
    public const string ClaveSinLimitesDePlan = "Testing:SinLimitesDePlan";
    private static readonly string[] ClavesObligatoriasIa = ["ApiKey", "BaseUrl", "Model"];

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton(CrearPlanSuscripcion(configuration));
        services.AddSingleton(new ModoPruebasConfig
        {
            SinLimitesDePlan = bool.TryParse(configuration[ClaveSinLimitesDePlan], out var sinLimites) && sinLimites
        });

        var mercadoPagoToken = configuration["MercadoPago:AccessToken"];
        if (!string.IsNullOrWhiteSpace(mercadoPagoToken))
        {
            MercadoPagoConfig.AccessToken = mercadoPagoToken;
        }

        services.AddScoped<IPaymentGatewayService, MercadoPagoPaymentService>();

        var seccionIa = ValidarConfiguracionIa(configuration);
        if (seccionIa == OpenAiClient.SeccionConfiguracion)
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

    private static string ValidarConfiguracionIa(IConfiguration configuration)
    {
        var proveedor = configuration["Ai:Provider"]?.Trim();
        var proveedoresValidos = $"'{OpenAiClient.NombreProveedor}' o '{GroqClient.NombreProveedor}'";

        if (string.IsNullOrWhiteSpace(proveedor))
        {
            throw new InvalidOperationException(
                $"Configuración de IA inválida: falta 'Ai:Provider'. Definilo como {proveedoresValidos} en appsettings.json o en User Secrets.");
        }

        string seccion;
        if (string.Equals(proveedor, OpenAiClient.NombreProveedor, StringComparison.OrdinalIgnoreCase))
        {
            seccion = OpenAiClient.SeccionConfiguracion;
        }
        else if (string.Equals(proveedor, GroqClient.NombreProveedor, StringComparison.OrdinalIgnoreCase))
        {
            seccion = GroqClient.SeccionConfiguracion;
        }
        else
        {
            throw new InvalidOperationException(
                $"Configuración de IA inválida: 'Ai:Provider' vale '{proveedor}'. Los valores válidos son {proveedoresValidos}.");
        }

        var faltantes = ClavesObligatoriasIa
            .Where(clave => string.IsNullOrWhiteSpace(configuration[$"{seccion}:{clave}"]))
            .Select(clave => $"'{seccion}:{clave}'")
            .ToList();

        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Configuración de IA incompleta para el proveedor '{proveedor}': falta {string.Join(", ", faltantes)}. " +
                "La API key va en User Secrets y el resto en appsettings.json.");
        }

        var temperatura = configuration[$"{seccion}:Temperature"];
        if (!string.IsNullOrWhiteSpace(temperatura) &&
            !double.TryParse(temperatura, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidOperationException(
                $"Configuración de IA inválida: '{seccion}:Temperature' vale '{temperatura}'. Usá un número con punto decimal, por ejemplo 0.3.");
        }

        if (!Uri.TryCreate(configuration[$"{seccion}:BaseUrl"], UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"Configuración de IA inválida: '{seccion}:BaseUrl' no es una URL válida.");
        }

        return seccion;
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
