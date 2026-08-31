using Kitch.Application.DTOs.Admin;
using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.DTOs.Usuarios;

namespace Kitch.Application.Interfaces;

public interface IAdminService
{
    Task<MetricasPlataformaDto> GetMetricasAsync(int usuarioId);
    Task<IEnumerable<UsuarioResponseDto>> GetUsuariosAsync(int usuarioId);
    Task<IEnumerable<SuscripcionResponseDto>> GetSuscripcionesAsync(int usuarioId);
}
