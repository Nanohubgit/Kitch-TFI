using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.Suscripciones;

public class PaymentGatewayRequest
{
    public int UsuarioId { get; set; }
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
}

public class PaymentGatewayResult
{
    public bool Aprobado { get; set; }
    public string? TransaccionId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

public class CheckoutPreferenceResult
{
    public string InitPoint { get; set; } = string.Empty;
    public string PreferenceId { get; set; } = string.Empty;
}

public class PaymentNotificationResult
{
    public bool Aprobado { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? TransaccionId { get; set; }
    public decimal Monto { get; set; }
}
