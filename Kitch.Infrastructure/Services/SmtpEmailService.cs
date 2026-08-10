using Kitch.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Kitch.Infrastructure.Services;

/// <summary>
/// Adaptador de Infrastructure: envía emails vía SMTP usando MailKit/MimeKit.
/// Application solo conoce <see cref="IEmailService"/>; esta clase es el detalle técnico.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _configuration["EmailSettings:Host"]
            ?? throw new InvalidOperationException("EmailSettings:Host no está configurado.");
        var portText = _configuration["EmailSettings:Port"] ?? "587";
        if (!int.TryParse(portText, out var port))
        {
            throw new InvalidOperationException("EmailSettings:Port debe ser un número válido.");
        }

        var user = _configuration["EmailSettings:User"] ?? string.Empty;
        var password = _configuration["EmailSettings:Password"] ?? string.Empty;
        var from = _configuration["EmailSettings:From"]
            ?? throw new InvalidOperationException("EmailSettings:From no está configurado.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();

        // Mailtrap / la mayoría de SMTP de desarrollo usan el puerto 587 con STARTTLS.
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);

        if (!string.IsNullOrWhiteSpace(user))
        {
            await client.AuthenticateAsync(user, password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
