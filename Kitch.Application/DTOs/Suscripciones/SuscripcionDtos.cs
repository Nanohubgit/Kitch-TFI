using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Suscripciones;

public class SuscripcionCreateDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activa { get; set; } = true;

    [Required, MaxLength(50)]
    public string Tipo { get; set; } = string.Empty;
}

public class SuscripcionUpdateDto : SuscripcionCreateDto
{
}

public class SuscripcionResponseDto
{
    public int Id { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Activa { get; set; }
    public string Tipo { get; set; } = string.Empty;
}
