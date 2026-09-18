using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Alacena;

public class AgregarIngredienteRequestDto
{
    public int IngredienteId { get; set; }

    [MaxLength(100)]
    public string? NombreIngrediente { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Required, MaxLength(50)]
    public string UnidadMedida { get; set; } = string.Empty;

    public DateTime? FechaCaducidad { get; set; }
}

public class ActualizarCantidadAlacenaRequestDto
{
    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [MaxLength(50)]
    public string? UnidadMedida { get; set; }
}

public class IngredienteAlacenaResponseDto
{
    public int Id { get; set; }
    public int IngredienteId { get; set; }
    public string NombreIngrediente { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public DateTime? FechaCaducidad { get; set; }
}
