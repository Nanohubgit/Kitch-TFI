namespace Kitch.Application.Interfaces;

public interface IAsistenteIaClient
{
    Task<string> GenerarRespuestaAsync(string prompt, string systemInstruction);

    Task<string> GenerarRespuestaJsonAsync(string prompt, string systemInstruction);

    Task<string> GenerarRespuestaConversacionAsync(
        IEnumerable<MensajeIa> mensajes,
        string systemInstruction,
        bool jsonMode = false);
}
