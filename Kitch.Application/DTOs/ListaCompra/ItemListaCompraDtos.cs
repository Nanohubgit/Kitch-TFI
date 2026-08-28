using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.ListaCompra;

public class ItemListaCompraCreateDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required, MaxLength(200)]
    public string NombreArticulo { get; set; } = string.Empty;

    [Range(0.01, float.MaxValue)]
    public float CantidadFaltante { get; set; }
}

public class ItemListaCompraUpdateDto
{
    [Required, MaxLength(200)]
    public string NombreArticulo { get; set; } = string.Empty;

    [Range(0.01, float.MaxValue)]
    public float CantidadFaltante { get; set; }

    public bool EstaComprado { get; set; }
}

public class ItemListaCompraResponseDto
{
    public int Id { get; set; }
    public string NombreArticulo { get; set; } = string.Empty;
    public float CantidadFaltante { get; set; }
    public bool EstaComprado { get; set; }
}
