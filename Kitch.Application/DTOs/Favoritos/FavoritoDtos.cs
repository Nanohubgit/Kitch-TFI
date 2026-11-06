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
    public int Id { get; set; }

    public int RecetaId { get; set; }

    public string UsuarioEmail { get; set; } = string.Empty;
    public string RecetaTitulo { get; set; } = string.Empty;
    public string RecetaCategoria { get; set; } = "general";
    public string RecetaDificultad { get; set; } = string.Empty;
    public int RecetaTiempoPreparacionMinutos { get; set; }
    public bool BloqueadaPorPlan { get; set; }
}

public class ToggleFavoritoResponseDto
{
    public bool EsFavorito { get; set; }
    public string Message { get; set; } = string.Empty;
}
