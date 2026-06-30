using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.StockUsuarios;

public class StockUsuarioCreateDto
{
    public int UsuarioId { get; set; }

    public int IngredienteId { get; set; }

    [MaxLength(100)]
    public string? NombreIngrediente { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Required, MaxLength(50)]
    public string UnidadMedida { get; set; } = string.Empty;

    public DateTime? FechaCaducidad { get; set; }
}

public class StockUsuarioUpdateDto
{
    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Required, MaxLength(50)]
    public string UnidadMedida { get; set; } = string.Empty;
}

public class StockUsuarioResponseDto
{
    public int Id { get; set; }
    public int IngredienteId { get; set; }
    public string NombreIngrediente { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public DateTime? FechaCaducidad { get; set; }
}
