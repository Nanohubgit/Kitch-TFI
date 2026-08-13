namespace Kitch.Application.Interfaces;

/// <summary>
/// Puerto de salida (Application) para el envío de correos electrónicos.
/// Aísla los casos de uso de detalles de infraestructura (SMTP, MailKit, SendGrid, etc.):
/// Application solo declara la necesidad de "enviar un email"; Infrastructure provee la implementación.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo electrónico al destinatario indicado.
    /// </summary>
    /// <param name="to">Dirección de correo del destinatario.</param>
    /// <param name="subject">Asunto del mensaje.</param>
    /// <param name="body">Cuerpo del mensaje (texto o HTML, según la implementación).</param>
    Task SendEmailAsync(string to, string subject, string body);
}
