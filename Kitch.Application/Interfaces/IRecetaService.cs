using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IRecetaService
{
    Task<IEnumerable<Receta>> GetAllAsync();
    Task<Receta?> GetByIdAsync(int id);
    Task<Receta> CreateAsync(Receta receta);
    Task<bool> UpdateAsync(int id, Receta receta);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Receta>> GetByDificultadAsync(DificultadReceta dificultad);
}
