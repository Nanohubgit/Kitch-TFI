using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.Recomendacion;

public class RecetaCompatibleDto
{
    public int RecetaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DificultadReceta Dificultad { get; set; }
    public int TiempoPreparacionMinutos { get; set; }
    public int CaloriasEstimadas { get; set; }
    public int Porciones { get; set; }

    public int TotalIngredientes { get; set; }
    public int IngredientesDisponibles { get; set; }
    public int PorcentajeCoincidencia { get; set; }
    public List<string> IngredientesFaltantes { get; set; } = new();
}
