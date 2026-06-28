using System.Threading.Tasks;

namespace Kitch.Domain.Interfaces;

public interface IGeminiClient
{
    Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction);

    // Igual que GenerarRespuestaAsync, pero le pide al modelo que responda únicamente
    // con JSON válido (responseMimeType=application/json) para poder parsearlo.
    Task<string> GenerarRespuestaJsonAsync(string prompt, string systemInstruction);
}
