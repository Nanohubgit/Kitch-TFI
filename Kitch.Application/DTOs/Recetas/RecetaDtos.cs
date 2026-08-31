using System.ComponentModel.DataAnnotations;
using Kitch.Domain.Entities;

namespace Kitch.Application.DTOs.Recetas;

public class IngredienteRecetaCreateDto
{
    [Required]
    public int IngredienteId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Required, MaxLength(50)]
    public string UnidadMedida { get; set; } = string.Empty;
}

public class IngredienteRecetaResponseDto
{
    public int Id { get; set; }
    public int IngredienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
}

public class PreparacionRecetaCreateDto
{
    [Range(1, int.MaxValue)]
    public int NumeroPaso { get; set; }

    [Required, MaxLength(1000)]
    public string DescripcionPaso { get; set; } = string.Empty;
}

public class PreparacionRecetaResponseDto
{
    public int Id { get; set; }
    public int NumeroPaso { get; set; }
    public string DescripcionPaso { get; set; } = string.Empty;
}

public class RecetaCreateDto
{
    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int CaloriasEstimadas { get; set; }

    [Required, MaxLength(1000)]
    public string Descripcion { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int TiempoPreparacionMinutos { get; set; }

    [Range(1, int.MaxValue)]
    public int Porciones { get; set; }

    [EnumDataType(typeof(DificultadReceta))]
    public DificultadReceta Dificultad { get; set; } = DificultadReceta.Medio;

    [MinLength(1)]
    public List<IngredienteRecetaCreateDto> Ingredientes { get; set; } = new();

    [MinLength(1)]
    public List<PreparacionRecetaCreateDto> Preparaciones { get; set; } = new();
}

public class RecetaUpdateDto : RecetaCreateDto
{
}

public class RecetaResponseDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int CaloriasEstimadas { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int TiempoPreparacionMinutos { get; set; }
    public int Porciones { get; set; }
    public DificultadReceta Dificultad { get; set; }
    public List<IngredienteRecetaResponseDto> Ingredientes { get; set; } = new();
    public List<PreparacionRecetaResponseDto> Preparaciones { get; set; } = new();
}
