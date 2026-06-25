using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Sustitutos;

public class SustitutoCreateDto
{
    [Required]
    public int IngredienteId { get; set; }

    [Required]
    public int SustitutoId { get; set; }

    [MaxLength(500)]
    public string? Motivo { get; set; }
}

public class SustitutoUpdateDto
{
    [Required]
    public int IngredienteId { get; set; }

    [Required]
    public int SustitutoId { get; set; }

    [MaxLength(500)]
    public string? Motivo { get; set; }
}

public class SustitutoResponseDto
{
    public string Ingrediente { get; set; } = string.Empty;
    public string Sustituto { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}
