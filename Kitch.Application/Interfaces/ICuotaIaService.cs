namespace Kitch.Application.Interfaces;

/// <summary>
/// Cupo diario de peticiones al asistente de IA según el plan.
/// Hay que llamarlo ANTES de pegarle al proveedor (Groq / OpenAI).
/// </summary>
public interface ICuotaIaService
{
    Task ConsumirAsync(int usuarioId);
}
