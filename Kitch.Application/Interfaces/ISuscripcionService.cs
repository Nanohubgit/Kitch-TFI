using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface ISuscripcionService
{
    Task<IEnumerable<Suscripcion>> GetAllAsync();
    Task<Suscripcion?> GetByIdAsync(int id);
    Task<Suscripcion> CreateAsync(Suscripcion suscripcion);
    Task<bool> UpdateAsync(int id, Suscripcion suscripcion);
    Task<bool> DeleteAsync(int id);
}
