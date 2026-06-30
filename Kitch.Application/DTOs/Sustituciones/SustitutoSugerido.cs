namespace Kitch.Application.DTOs.Sustituciones;

public class SustitutoSugerido
{
    public int IngredienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal FactorEquivalencia { get; set; }
    public string? Notas { get; set; }

    public bool DisponibleEnAlacena { get; set; }
}
