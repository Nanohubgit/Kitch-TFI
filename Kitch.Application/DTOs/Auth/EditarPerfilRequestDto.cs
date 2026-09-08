using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Edición de perfil desde la UI: solo handle y preferencia dietética.
/// </summary>
public class EditarPerfilRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre de usuario debe tener al menos 3 caracteres.")]
    [MaxLength(50, ErrorMessage = "El nombre de usuario no puede superar 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "El nombre de usuario solo permite letras, números, punto, guion y guion bajo.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La preferencia dietética es obligatoria.")]
    [MaxLength(50)]
    public string PreferenciaDietetica { get; set; } = "Ninguna";
}
