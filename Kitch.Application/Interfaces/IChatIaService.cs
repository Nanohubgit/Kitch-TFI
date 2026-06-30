using System.Threading.Tasks;
using Kitch.Application.DTOs.ChatIa;

namespace Kitch.Application.Interfaces;

public interface IChatIaService
{
    Task<ChatRespuestaDto> ProcesarMensajeAsync(int usuarioId, ChatRequestDto request);
}
