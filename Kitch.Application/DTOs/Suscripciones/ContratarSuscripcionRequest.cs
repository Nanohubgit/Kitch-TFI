using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.Suscripciones;

public class ContratarSuscripcionRequest
{
    public string Tipo { get; set; } = "Profesional";
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; } = MetodoPago.TarjetaCredito;
}
