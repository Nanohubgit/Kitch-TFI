using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Favoritos;

public class FavoritoCreateDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int RecetaId { get; set; }
}

public class FavoritoResponseDto
{
    public string UsuarioEmail { get; set; } = string.Empty;
    public string RecetaTitulo { get; set; } = string.Empty;
}
