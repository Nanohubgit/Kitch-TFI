using System.Threading.Tasks;

namespace Kitch.Domain.Interfaces;

public interface IGeminiClient
{
    Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction);
}
