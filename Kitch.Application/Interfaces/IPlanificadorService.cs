using Kitch.Application.DTOs.Planificador;

namespace Kitch.Application.Interfaces;

public interface IPlanificadorService
{
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByFechaAsync(int usuarioId, DateTime fecha);
    Task<ComidaPlanificadaResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<ComidaPlanificadaResponseDto> CreateAsync(ComidaPlanificadaCreateDto comida);
    Task<PlanificacionResultadoDto> PlanificarAsync(ComidaPlanificadaCreateDto comida);
    Task<bool> UpdateAsync(int id, ComidaPlanificadaUpdateDto comida, int usuarioId);
    Task<bool> DeleteAsync(int id, int usuarioId);
}
