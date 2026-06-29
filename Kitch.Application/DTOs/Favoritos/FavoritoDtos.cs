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
    // Id del favorito: se usa para DELETE /api/Favoritos/{id} (quita la receta de tus favoritos).
    public int Id { get; set; }

    // Id de la receta: se usa para DELETE /api/Recetas/{id} (borra la receta del catálogo).
    public int RecetaId { get; set; }

    public string UsuarioEmail { get; set; } = string.Empty;
    public string RecetaTitulo { get; set; } = string.Empty;
}
