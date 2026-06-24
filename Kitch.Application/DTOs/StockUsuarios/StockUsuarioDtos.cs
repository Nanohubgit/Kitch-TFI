using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.StockUsuarios;

public class StockUsuarioCreateDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int IngredienteId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Required, MaxLength(50)]
    public string UnidadMedida { get; set; } = string.Empty;
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
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
}
