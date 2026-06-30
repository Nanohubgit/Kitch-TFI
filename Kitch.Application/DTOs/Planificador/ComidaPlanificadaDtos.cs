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
}

public class ComidaPlanificadaUpdateDto : ComidaPlanificadaCreateDto
{
}

public class ComidaPlanificadaResponseDto
{
    public int Id { get; set; }
    public DateTime FechaAsignada { get; set; }
    public string Turno { get; set; } = string.Empty;
}

public class PlanificacionResultadoDto
{
    public ComidaPlanificadaResponseDto Comida { get; set; } = new();
    public List<string> IngredientesAgregadosALista { get; set; } = [];
}
