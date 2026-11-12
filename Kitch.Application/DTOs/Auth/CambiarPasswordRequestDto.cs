using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

public class CambiarPasswordRequestDto
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
    public string NuevaPassword { get; set; } = string.Empty;
}
