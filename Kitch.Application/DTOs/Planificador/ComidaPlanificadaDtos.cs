using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Planificador;

public class ComidaPlanificadaCreateDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int RecetaId { get; set; }

    [Required]
    public DateTime FechaAsignada { get; set; }

    [Required, MaxLength(50)]
    public string Turno { get; set; } = string.Empty;

    public bool ReemplazarSiConflicto { get; set; }
}

public class ComidaPlanificadaUpdateDto : ComidaPlanificadaCreateDto
{
}

public class AgendarRecetaRequestDto
{
    [Required]
    public int RecetaId { get; set; }

    [Required]
    public DateTime FechaAsignada { get; set; }

    [Required, MaxLength(50)]
    public string Turno { get; set; } = string.Empty;

    public bool ReemplazarSiConflicto { get; set; }
}

public class ComidaPlanificadaResponseDto
{
    public int Id { get; set; }
    public int RecetaId { get; set; }
    public string RecetaTitulo { get; set; } = string.Empty;
    public DateTime FechaAsignada { get; set; }
    public string Turno { get; set; } = string.Empty;
    public List<string> IngredientesAgregadosALista { get; set; } = [];
    public bool TeniaTodoEnAlacena { get; set; }
    public bool ReemplazoRealizado { get; set; }
    public string MensajeLista { get; set; } = string.Empty;
}

public class PlanificacionResultadoDto
{
    public ComidaPlanificadaResponseDto Comida { get; set; } = new();
    public List<string> IngredientesAgregadosALista { get; set; } = [];
}
