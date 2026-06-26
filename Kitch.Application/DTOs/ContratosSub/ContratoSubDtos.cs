using System.ComponentModel.DataAnnotations;
using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.ContratosSub;

public class ContratoSubCreateDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int SuscripcionId { get; set; }

    [Required]
    public DateTime FechaInicio { get; set; }

    [Required]
    public DateTime FechaFin { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }
}

public class ContratoSubUpdateDto
{
    [Required]
    public DateTime FechaInicio { get; set; }

    [Required]
    public DateTime FechaFin { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }

    [EnumDataType(typeof(EstadoContratoSub))]
    public EstadoContratoSub Estado { get; set; }
}

public class ContratoSubResponseDto
{
    public DateTime FechaContratacion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal Monto { get; set; }
    public EstadoContratoSub Estado { get; set; }
    public int DiasRestantes { get; set; }
}
