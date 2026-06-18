using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.Suscripciones;

public class ContratarSuscripcionRequest
{
    public string Tipo { get; set; } = "Profesional";
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; } = MetodoPago.TarjetaCredito;

    // Sin pasarela real: permite forzar el flujo alternativo de pago rechazado para la demo.
    public bool SimularRechazo { get; set; }
}
