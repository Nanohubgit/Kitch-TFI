using Kitch.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kitch.Infrastructure.Services;

/// <summary>
/// Factory sencilla: lee "PaymentGateway" de appsettings y devuelve
/// la implementación dummy/real correspondiente.
/// </summary>
public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    public const string Stripe = "Stripe";
    public const string MercadoPago = "MercadoPago";

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public PaymentGatewayFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public IPaymentGatewayService Create()
    {
        var gateway = _configuration["PaymentGateway"]?.Trim();

        return gateway?.Equals(Stripe, StringComparison.OrdinalIgnoreCase) == true
            ? _serviceProvider.GetRequiredService<StripePaymentService>()
            : _serviceProvider.GetRequiredService<MercadoPagoPaymentService>();
    }
}
