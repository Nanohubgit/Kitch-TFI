using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.Suscripciones;

public class ContratarSuscripcionResult
{
    public bool Aprobado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string RolUsuario { get; set; } = string.Empty;
    public int ContratoId { get; set; }
    public int PagoId { get; set; }
    public EstadoPago EstadoPago { get; set; }
}
