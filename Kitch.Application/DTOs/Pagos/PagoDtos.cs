using System.ComponentModel.DataAnnotations;
using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.Pagos;

public class PagoCreateDto
{
    [Required]
    public int ContratoSubId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }

    [EnumDataType(typeof(MetodoPago))]
    public MetodoPago MetodoPago { get; set; } = MetodoPago.NoEspecificado;
}

public class PagoUpdateDto
{
    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }

    [EnumDataType(typeof(EstadoPago))]
    public EstadoPago EstadoPago { get; set; }

    [EnumDataType(typeof(MetodoPago))]
    public MetodoPago MetodoPago { get; set; }
}

public class PagoResponseDto
{
    public DateTime FechaPago { get; set; }
    public decimal Monto { get; set; }
    public EstadoPago EstadoPago { get; set; }
    public MetodoPago MetodoPago { get; set; }
}
