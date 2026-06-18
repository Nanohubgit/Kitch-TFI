namespace Kitch.Application.DTOs.Sustituciones;

public class SustitutoSugerido
{
    public int IngredienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal FactorEquivalencia { get; set; }
    public string? Notas { get; set; }

    // True si el usuario ya tiene este sustituto en su Alacena Virtual.
    public bool DisponibleEnAlacena { get; set; }
}
