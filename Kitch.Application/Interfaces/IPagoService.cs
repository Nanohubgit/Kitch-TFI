using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IPagoService
{
    Task<IEnumerable<Pago>> GetAllAsync();
    Task<IEnumerable<Pago>> GetByUsuarioIdAsync(int usuarioId);
    Task<Pago?> GetByIdAsync(int id);
    Task<Pago> CreateAsync(Pago pago);
    Task<bool> UpdateAsync(int id, Pago pago);
    Task<bool> DeleteAsync(int id);
}
