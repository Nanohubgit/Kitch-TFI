using System.Text.Json.Serialization;
using Kitch.Application.DTOs.Planificador;
using Kitch.Application.DTOs.Recomendacion;
using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.DTOs.Sustituciones;

namespace Kitch.Application.DTOs.ChatIa;

public class ChatTurnoDto
{
    [JsonPropertyName("rol")]
    public string Rol { get; set; } = "usuario";

    [JsonPropertyName("texto")]
    public string Texto { get; set; } = string.Empty;
}

public class ChatRequestDto
{
    public string Mensaje { get; set; } = string.Empty;

    public List<ChatTurnoDto>? Historial { get; set; }

    public RecetaGeneradaDto? RecetaActual { get; set; }
}

public static class ChatAccion
{
    public const string Conversar = "conversar";
    public const string GenerarReceta = "generar_receta";
    public const string GuardarReceta = "guardar_receta";
    public const string Sustituir = "sustituir";
    public const string Recomendar = "recomendar";
    public const string EliminarReceta = "eliminar_receta";
    public const string PlanificarReceta = "planificar_receta";
    public const string CocinarReceta = "cocinar_receta";
    public const string ConsultarRecetasGuardadas = "consultar_recetas_guardadas";
}

public class ChatRespuestaDto
{
    public string Accion { get; set; } = ChatAccion.Conversar;

    public string Mensaje { get; set; } = string.Empty;

    public RecetaGeneradaDto? Receta { get; set; }

    public RecetaGuardadaResponse? RecetaGuardada { get; set; }

    public string? IngredienteSustituido { get; set; }

    public List<SustitutoSugerido>? Sustitutos { get; set; }

    public List<RecetaCompatibleDto>? Recomendaciones { get; set; }

    public int? RecetasEliminadas { get; set; }

    public int? CantidadRecetasGuardadas { get; set; }

    public ComidaPlanificadaResponseDto? ComidaPlanificada { get; set; }

    public List<string>? IngredientesAgregadosALista { get; set; }

    public List<string>? StockDescontado { get; set; }

    public List<string>? StockFaltante { get; set; }
}
