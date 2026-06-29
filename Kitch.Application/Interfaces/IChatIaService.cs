using System.Threading.Tasks;
using Kitch.Application.DTOs.ChatIa;

namespace Kitch.Application.Interfaces;

public interface IChatIaService
{
    // Punto de entrada único del asistente. Según lo que pida el usuario en lenguaje
    // natural, el agente conversa, genera una receta, la guarda, sugiere sustitutos
    // o recomienda recetas, y ejecuta los efectos correspondientes (catálogo, favoritos,
    // sustitutos), todo dentro de la misma conversación.
    Task<ChatRespuestaDto> ProcesarMensajeAsync(int usuarioId, ChatRequestDto request);
}
