using System.ComponentModel.DataAnnotations;
using Kitch.Application.DTOs.Recetas;

namespace Kitch.Application.DTOs.Preparacion;

public class PrevisualizarPorcionesRequestDto
{
    public int UsuarioId { get; set; }

    [Required]
    public int RecetaId { get; set; }

    [Range(1, int.MaxValue)]
    public int NuevasPorciones { get; set; }
}

public class IngredienteAjustadoDto
{
    public int IngredienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal CantidadOriginal { get; set; }
    public decimal CantidadAjustada { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal CantidadEnAlacena { get; set; }
    public decimal CantidadFaltante { get; set; }
    public bool AlcanzaStock { get; set; }
    public string? UnidadEnAlacena { get; set; }
    public bool UnidadCompatible { get; set; } = true;
}

public class PrevisualizarPorcionesResponseDto
{
    public string Receta { get; set; } = string.Empty;
    public int PorcionesOriginales { get; set; }
    public int NuevasPorciones { get; set; }
    public bool AlcanzaStock { get; set; }
    public bool HayFaltantes { get; set; }
    public List<IngredienteAjustadoDto> Ingredientes { get; set; } = new();
    public List<IngredienteAjustadoDto> IngredientesFaltantes { get; set; } = new();
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

    public List<CantidadUtilizadaDto> CantidadesUtilizadas { get; set; } = new();
}

public class CantidadUtilizadaDto
{
    [Required]
    public int IngredienteId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }
}

public class AgregarFaltantesPorcionesRequestDto
{
    [Required]
    public int RecetaId { get; set; }

    [Range(1, int.MaxValue)]
    public int NuevasPorciones { get; set; }
}

public class AplicarSustitutoRequestDto
{
    [Required]
    public int RecetaId { get; set; }

    [Required]
    public int IngredienteOriginalId { get; set; }

    [Required]
    public int IngredienteSustitutoId { get; set; }
}

public class AplicarSustitutoResponseDto
{
    public int RecetaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public List<IngredienteRecetaResponseDto> Ingredientes { get; set; } = new();
}

public class IngredienteDescuentoDto
{
    public int IngredienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal CantidadDisponible { get; set; }
    public decimal CantidadNecesaria { get; set; }
    public decimal CantidadPosterior { get; set; }
    public decimal CantidadFaltante { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public string? UnidadEnAlacena { get; set; }
    public bool UnidadCompatible { get; set; } = true;
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
    public string Mensaje { get; set; } = string.Empty;
    public List<IngredienteMovimientoStockDto> Descontados { get; set; } = new();
    public List<IngredienteMovimientoStockDto> Faltantes { get; set; } = new();
    public List<IngredienteMovimientoStockDto> NoDescontados { get; set; } = new();
}

public class IngredienteMovimientoStockDto
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
}
