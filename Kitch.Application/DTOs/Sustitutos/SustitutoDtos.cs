using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Sustitutos;

public class SustitutoCreateDto
{
    [Required]
    public int IngredienteOriginalId { get; set; }

    [Required]
    public int IngredienteSustitutoId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El factor de equivalencia debe ser mayor a cero.")]
    public decimal FactorEquivalencia { get; set; } = 1m;

    [MaxLength(250)]
    public string? Notas { get; set; }
}

public class SustitutoUpdateDto
{
    [Required]
    public int IngredienteOriginalId { get; set; }

    [Required]
    public int IngredienteSustitutoId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El factor de equivalencia debe ser mayor a cero.")]
    public decimal FactorEquivalencia { get; set; } = 1m;

    [MaxLength(250)]
    public string? Notas { get; set; }
}

public class SustitutoResponseDto
{
    public int Id { get; set; }
    public string IngredienteOriginal { get; set; } = string.Empty;
    public string IngredienteSustituto { get; set; } = string.Empty;
    public decimal FactorEquivalencia { get; set; }
    public string? Notas { get; set; }
}
