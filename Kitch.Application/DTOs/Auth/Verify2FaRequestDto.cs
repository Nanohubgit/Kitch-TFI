using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

public class Verify2FaRequestDto
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código de verificación es obligatorio.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener exactamente 6 dígitos.")]
    public string Codigo { get; set; } = string.Empty;
}
