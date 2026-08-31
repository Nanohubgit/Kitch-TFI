namespace Kitch.Application.DTOs.Admin;

public class MetricasPlataformaDto
{
    public int UsuariosTotales { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsuariosBasicos { get; set; }
    public int UsuariosProfesionales { get; set; }
    public int UsuariosAdmin { get; set; }
    public int SuscripcionesTotales { get; set; }
    public int SuscripcionesActivas { get; set; }
    public int ContratosActivos { get; set; }
    public int PagosAprobados { get; set; }
    public decimal IngresosTotales { get; set; }
}
