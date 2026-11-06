namespace Kitch.Application.DTOs.Auth;

public class Login2FaResponseDto
{
    public bool RequiresTwoFactor { get; set; } = true;

    public string Email { get; set; } = string.Empty;

    public string EmailEnmascarado { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;
}
