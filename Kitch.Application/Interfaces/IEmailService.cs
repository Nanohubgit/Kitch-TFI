namespace Kitch.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);

    Task SendEmailAsync(string to, string subject, string textBody, string htmlBody);
}
