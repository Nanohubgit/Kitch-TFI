using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Kitch.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private const int TimeoutMilisegundos = 20000;
    private const string RemitentePorDefecto = "no-reply@alacenavirtual.com";
    private const string MensajeNoDisponible =
        "No pudimos enviar el correo en este momento. Intentá nuevamente en unos minutos.";

    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _configuration["EmailSettings:Host"]?.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogError("Falta configurar EmailSettings:Host");
            throw new ServicioExternoException("El envío de correos no está configurado. Contactá a un administrador.");
        }

        var portText = _configuration["EmailSettings:Port"];
        var port = int.TryParse(portText, out var puertoConfigurado) && puertoConfigurado > 0
            ? puertoConfigurado
            : 587;

        var user = _configuration["EmailSettings:User"]?.Trim() ?? string.Empty;
        var password = _configuration["EmailSettings:Password"] ?? string.Empty;
        var from = _configuration["EmailSettings:From"]?.Trim();
        if (string.IsNullOrWhiteSpace(from))
        {
            from = user.Contains('@') ? user : RemitentePorDefecto;
        }

        if (!MailboxAddress.TryParse(to, out var destinatario))
        {
            throw new InvalidOperationException("El email de destino no es válido.");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.TryParse(from, out var remitente)
            ? remitente
            : MailboxAddress.Parse(RemitentePorDefecto));
        message.To.Add(destinatario);
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient { Timeout = TimeoutMilisegundos };

        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.Auto);

            if (!string.IsNullOrWhiteSpace(user))
            {
                await client.AuthenticateAsync(user, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException or AuthenticationException
                                       or ServiceNotConnectedException or SslHandshakeException
                                       or System.Net.Sockets.SocketException or IOException or TimeoutException
                                       or OperationCanceledException)
        {
            _logger.LogError(ex, "Falló el envío de correo a través de {Host}:{Port}", host, port);
            throw new ServicioExternoException(MensajeNoDisponible, ex);
        }
    }
}
