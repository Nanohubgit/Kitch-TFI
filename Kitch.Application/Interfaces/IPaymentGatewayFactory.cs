namespace Kitch.Application.Interfaces;

/// <summary>
/// Resuelve la implementación concreta de IPaymentGatewayService
/// según la configuración activa (Stripe, MercadoPago, etc.).
/// </summary>
public interface IPaymentGatewayFactory
{
    IPaymentGatewayService Create();
}
