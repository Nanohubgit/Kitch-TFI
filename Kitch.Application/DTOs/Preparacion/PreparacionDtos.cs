using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Preparacion;

public class PrevisualizarPorcionesRequestDto
{
    [Required]
    public int RecetaId { get; set; }

    [Range(1, int.MaxValue)]
    public int NuevasPorciones { get; set; }
}

public class IngredienteAjustadoDto
{
    public decimal CantidadOriginal { get; set; }
    public decimal CantidadAjustada { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
}

public class PrevisualizarPorcionesResponseDto
{
    public string Receta { get; set; } = string.Empty;
    public int PorcionesOriginales { get; set; }
    public int NuevasPorciones { get; set; }
    public List<IngredienteAjustadoDto> Ingredientes { get; set; } = new();
}

public class PrevisualizarDescuentoStockRequestDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int RecetaId { get; set; }

    [Range(1, int.MaxValue)]
    public int PorcionesCocinadas { get; set; }
}

public class ConfirmarDescuentoStockRequestDto
{
    [Required]
    public int RecetaId { get; set; }

    [Range(1, int.MaxValue)]
    public int PorcionesCocinadas { get; set; }
}

public class IngredienteDescuentoDto
{
    public decimal CantidadDisponible { get; set; }
    public decimal CantidadNecesaria { get; set; }
    public decimal CantidadPosterior { get; set; }
    public decimal CantidadFaltante { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
}

public class PrevisualizarDescuentoStockResponseDto
{
    public string Receta { get; set; } = string.Empty;
    public int PorcionesCocinadas { get; set; }
    public List<IngredienteDescuentoDto> Ingredientes { get; set; } = new();
}

public class DescuentoStockResultadoDto
{
    public string Receta { get; set; } = string.Empty;
    public List<IngredienteMovimientoStockDto> Descontados { get; set; } = new();
    public List<IngredienteMovimientoStockDto> Faltantes { get; set; } = new();
}

public class IngredienteMovimientoStockDto
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
}
