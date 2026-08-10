using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Auth;

/// <summary>
/// Datos de registro de un usuario nuevo.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de usuario único en el sistema (no confundir con el nombre real).
    /// </summary>
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre de usuario debe tener al menos 3 caracteres.")]
    [MaxLength(50, ErrorMessage = "El nombre de usuario no puede superar 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "El nombre de usuario solo permite letras, números, punto, guion y guion bajo.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Preferencia dietética: Ninguna, Vegano, Celiaco, etc.
    /// </summary>
    [Required(ErrorMessage = "La preferencia dietética es obligatoria.")]
    [MaxLength(50)]
    public string PreferenciaDietetica { get; set; } = "Ninguna";
}
