using Kitch.Application.DTOs.Planificador;

namespace Kitch.Application.Interfaces;

public interface IPlanificadorService
{
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByFechaAsync(int usuarioId, DateTime fecha);
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByRangoFechasAsync(int usuarioId, DateTime desde, DateTime hasta);
    Task<ComidaPlanificadaResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<ComidaPlanificadaResponseDto> CreateAsync(ComidaPlanificadaCreateDto comida);
    Task<ComidaPlanificadaResponseDto> AgendarAsync(int usuarioId, AgendarRecetaRequestDto request);
    Task<PlanificacionResultadoDto> PlanificarAsync(ComidaPlanificadaCreateDto comida);
    Task<bool> UpdateAsync(int id, ComidaPlanificadaUpdateDto comida, int usuarioId);
    Task<bool> DeleteAsync(int id, int usuarioId);
}
