using System.Text.Json.Serialization;
using Kitch.Application.DTOs.Recomendacion;
using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.DTOs.Sustituciones;

namespace Kitch.Application.DTOs.ChatIa;

// Un turno previo de la conversación, tal como lo reenvía el cliente para mantener contexto.
public class ChatTurnoDto
{
    // "usuario" o "asistente".
    [JsonPropertyName("rol")]
    public string Rol { get; set; } = "usuario";

    [JsonPropertyName("texto")]
    public string Texto { get; set; } = string.Empty;
}

// Lo que el cliente manda en cada turno del chat unificado.
public class ChatRequestDto
{
    // El mensaje nuevo que escribe el usuario.
    public string Mensaje { get; set; } = string.Empty;

    // El historial de la conversación (turnos previos). Permite que la IA recuerde,
    // por ejemplo, la receta que generó antes cuando el usuario pide "guardala".
    public List<ChatTurnoDto>? Historial { get; set; }

    // La receta que el usuario tiene "en pantalla" (último borrador generado).
    // Sirve de respaldo para guardar aunque la IA no la repita en su respuesta.
    public RecetaGeneradaDto? RecetaActual { get; set; }
}

// Acciones que el agente puede resolver dentro del mismo endpoint.
public static class ChatAccion
{
    public const string Conversar = "conversar";
    public const string GenerarReceta = "generar_receta";
    public const string GuardarReceta = "guardar_receta";
    public const string Sustituir = "sustituir";
    public const string Recomendar = "recomendar";
    public const string EliminarReceta = "eliminar_receta";
}

// Respuesta unificada del chat. Según la acción se completan distintos campos,
// pero el front siempre recibe la misma forma y muestra lo que venga cargado.
public class ChatRespuestaDto
{
    // Qué hizo el agente en este turno (ver ChatAccion).
    public string Accion { get; set; } = ChatAccion.Conversar;

    // Texto conversacional para mostrarle al usuario.
    public string Mensaje { get; set; } = string.Empty;

    // Borrador de receta generada (acción generar_receta). Sin guardar todavía.
    public RecetaGeneradaDto? Receta { get; set; }

    // Resultado de persistir la receta (acción guardar_receta).
    public RecetaGuardadaResponse? RecetaGuardada { get; set; }

    // Ingrediente para el que se buscaron sustitutos (acción sustituir).
    public string? IngredienteSustituido { get; set; }

    // Sustitutos sugeridos y persistidos (acción sustituir).
    public List<SustitutoSugerido>? Sustitutos { get; set; }

    // Recetas recomendadas según la alacena (acción recomendar).
    public List<RecetaCompatibleDto>? Recomendaciones { get; set; }

    // Cantidad de recetas borradas (acción eliminar_receta).
    public int? RecetasEliminadas { get; set; }
}
