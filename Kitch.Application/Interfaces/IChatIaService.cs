using System.Threading.Tasks;

namespace Kitch.Application.Interfaces;

public interface IChatIaService
{
    Task<string> EnviarMensajeChatAsync(int usuarioId, string mensajeUsuario);
}
