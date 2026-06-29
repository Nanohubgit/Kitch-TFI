using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.StockUsuarios;

public class StockUsuarioCreateDto
{
    // Lo setea el controller a partir del token; el cliente no lo manda.
    public int UsuarioId { get; set; }

    // Forma 1: si ya sabés el id del ingrediente del catálogo, lo pasás acá.
    public int IngredienteId { get; set; }

    // Forma 2 (recomendada para el usuario): cargás por nombre y el sistema
    // resuelve el ingrediente del catálogo o lo crea si no existe.
    [MaxLength(100)]
    public string? NombreIngrediente { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Required, MaxLength(50)]
    public string UnidadMedida { get; set; } = string.Empty;

    // Opcional: fecha de caducidad estimada del ingrediente.
    public DateTime? FechaCaducidad { get; set; }
}

public class StockUsuarioUpdateDto
{
    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Required, MaxLength(50)]
    public string UnidadMedida { get; set; } = string.Empty;
}

public class StockUsuarioResponseDto
{
    public int Id { get; set; }
    public int IngredienteId { get; set; }
    public string NombreIngrediente { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public DateTime? FechaCaducidad { get; set; }
}
