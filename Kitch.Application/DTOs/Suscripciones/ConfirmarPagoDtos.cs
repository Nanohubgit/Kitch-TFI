using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Suscripciones;

public class ConfirmarPagoRequestDto
{
    [Required, MaxLength(50)]
    public string PaymentId { get; set; } = string.Empty;
}

public class EstadoPagoResponseDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Aprobado { get; set; }
    public bool Rechazado { get; set; }
    public bool Pendiente { get; set; }
    public string RolActual { get; set; } = string.Empty;
    public bool RequiereRefrescarSesion { get; set; }
    public DateTime? ProfesionalHasta { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
