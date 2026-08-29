using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.Suscripciones;

/// <summary>
/// Datos necesarios para solicitar un cobro a la pasarela (agnóstico del proveedor).
/// </summary>
public class PaymentGatewayRequest
{
    public int UsuarioId { get; set; }
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
}

/// <summary>
/// Resultado estandarizado del intento de cobro.
/// </summary>
public class PaymentGatewayResult
{
    public bool Aprobado { get; set; }
    public string? TransaccionId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// Preferencia de checkout (Checkout Pro). El front redirige a <see cref="InitPoint"/>.
/// </summary>
public class CheckoutPreferenceResult
{
    public string InitPoint { get; set; } = string.Empty;
    public string PreferenceId { get; set; } = string.Empty;
}

/// <summary>
/// Estado de un pago consultado en la pasarela (agnóstico del proveedor).
/// </summary>
public class PaymentNotificationResult
{
    public bool Aprobado { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? TransaccionId { get; set; }
    public decimal Monto { get; set; }
}
