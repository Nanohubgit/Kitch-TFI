using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kitch.Domain.Interfaces;

public interface IGeminiClient
{
    Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction);

    // Igual que GenerarRespuestaAsync, pero le pide al modelo que responda únicamente
    // con JSON válido (responseMimeType=application/json) para poder parsearlo.
    Task<string> GenerarRespuestaJsonAsync(string prompt, string systemInstruction);

    // Conversación multi-turno: se le pasa todo el historial (usuario + asistente) para
    // que el modelo mantenga el contexto (ej: la receta que generó en un turno anterior).
    // jsonMode=true fuerza al modelo a responder únicamente con JSON válido.
    Task<string> GenerarRespuestaConversacionAsync(
        IEnumerable<MensajeIa> mensajes,
        string systemInstruction,
        bool jsonMode = false);
}
