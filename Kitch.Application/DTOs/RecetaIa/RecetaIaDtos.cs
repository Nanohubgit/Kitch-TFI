using System.Text.Json.Serialization;

namespace Kitch.Application.DTOs.RecetaIa;

public class GenerarRecetaRequest
{
    public string? Preferencias { get; set; }
}

public class IngredienteGeneradoDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; set; }

    [JsonPropertyName("unidadMedida")]
    public string UnidadMedida { get; set; } = string.Empty;
}

public class RecetaGeneradaDto
{
    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("tiempoPreparacionMinutos")]
    public int TiempoPreparacionMinutos { get; set; }

    [JsonPropertyName("porciones")]
    public int Porciones { get; set; }

    [JsonPropertyName("dificultad")]
    public string Dificultad { get; set; } = "Medio";

    [JsonPropertyName("caloriasEstimadas")]
    public int CaloriasEstimadas { get; set; }

    [JsonPropertyName("ingredientes")]
    public List<IngredienteGeneradoDto> Ingredientes { get; set; } = [];

    [JsonPropertyName("pasos")]
    public List<string> Pasos { get; set; } = [];
}

public class RecetaGuardadaResponse
{
    public int RecetaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public bool Favorita { get; set; } = true;
    public string Mensaje { get; set; } = string.Empty;
}
