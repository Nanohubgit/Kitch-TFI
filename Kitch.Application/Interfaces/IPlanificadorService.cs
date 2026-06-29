using Kitch.Application.DTOs.Planificador;

namespace Kitch.Application.Interfaces;

public interface IPlanificadorService
{
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByUsuarioIdAsync(int usuarioId);
    Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByFechaAsync(int usuarioId, DateTime fecha);
    // GetById, Update y Delete reciben el usuarioId del token para verificar que la comida
    // planificada le pertenezca (evita IDOR: ver/editar/borrar el planificador de otro).
    Task<ComidaPlanificadaResponseDto?> GetByIdAsync(int id, int usuarioId);
    Task<ComidaPlanificadaResponseDto> CreateAsync(ComidaPlanificadaCreateDto comida);
    // Igual que CreateAsync pero devuelve también qué ingredientes se sumaron a la lista
    // de compras (lo usa el chat para avisarle al usuario qué le faltaba).
    Task<PlanificacionResultadoDto> PlanificarAsync(ComidaPlanificadaCreateDto comida);
    Task<bool> UpdateAsync(int id, ComidaPlanificadaUpdateDto comida, int usuarioId);
    Task<bool> DeleteAsync(int id, int usuarioId);
}
