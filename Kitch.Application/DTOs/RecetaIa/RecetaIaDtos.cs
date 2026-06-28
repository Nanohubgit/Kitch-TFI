using System.Text.Json.Serialization;

namespace Kitch.Application.DTOs.RecetaIa;

// Lo que el usuario manda para pedirle a la IA que genere una receta con su alacena.
public class GenerarRecetaRequest
{
    public string? Preferencias { get; set; }
}

// Un ingrediente tal como lo devuelve la IA (nombre + cantidad + unidad).
public class IngredienteGeneradoDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; set; }

    [JsonPropertyName("unidadMedida")]
    public string UnidadMedida { get; set; } = string.Empty;
}

// La receta estructurada que genera la IA. Es la misma forma que se devuelve al usuario
// (borrador, sin guardar) y la que el usuario reenvía si decide guardarla.
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

// Respuesta al guardar: el id de la receta creada y favoriteada.
public class RecetaGuardadaResponse
{
    public int RecetaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public bool Favorita { get; set; } = true;
    public string Mensaje { get; set; } = string.Empty;
}
