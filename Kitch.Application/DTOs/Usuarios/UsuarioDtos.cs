using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Usuarios;

public class UsuarioCreateDto
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required, MinLength(3), MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string PreferenciaDietetica { get; set; } = "Ninguna";

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class UsuarioUpdateDto
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}

public class ActualizarPerfilDto
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}

public class CambiarRolDto
{
    [Required, MaxLength(50)]
    public string NuevoRol { get; set; } = string.Empty;
}

public class UsuarioResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PreferenciaDietetica { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public string Rol { get; set; } = string.Empty;
}
