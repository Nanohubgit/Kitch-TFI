using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IPlanificadorService
{
    Task<IEnumerable<ComidaPlanificada>> GetByUsuarioIdAsync(int usuarioId);
    Task<IEnumerable<ComidaPlanificada>> GetByFechaAsync(int usuarioId, DateTime fecha);
    Task<ComidaPlanificada?> GetByIdAsync(int id);
    Task<ComidaPlanificada> CreateAsync(ComidaPlanificada comida);
    Task<bool> UpdateAsync(int id, ComidaPlanificada comida);
    Task<bool> DeleteAsync(int id);
}
