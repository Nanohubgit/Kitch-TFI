using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

public class ReenviarCodigoRequestDto
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    public string Email { get; set; } = string.Empty;
}
