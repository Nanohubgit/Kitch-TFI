namespace Kitch.Domain.Interfaces;

// Un turno de la conversación que se le envía al modelo.
// Rol: "user" para el usuario, "model" para respuestas previas del asistente.
public record MensajeIa(string Rol, string Texto);
