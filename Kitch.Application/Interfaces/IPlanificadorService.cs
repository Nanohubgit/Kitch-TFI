using Kitch.Application.DTOs.Planificador;

namespace Kitch.Application.Interfaces;

public interface IPlanificadorService
{
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByFechaAsync(int usuarioId, DateTime fecha);
    Task<ComidaPlanificadaResponseDto?> GetByIdAsync(int id);
    Task<ComidaPlanificadaResponseDto> CreateAsync(ComidaPlanificadaCreateDto comida);
    Task<bool> UpdateAsync(int id, ComidaPlanificadaUpdateDto comida);
    Task<bool> DeleteAsync(int id);
}
