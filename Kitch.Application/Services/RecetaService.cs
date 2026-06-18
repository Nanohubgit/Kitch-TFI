using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class RecetaService : IRecetaService
{
    private readonly IRepository<Receta> _repository;

    public RecetaService(IRepository<Receta> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Receta>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Receta?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Receta> CreateAsync(Receta receta)
    {
        return await _repository.AddAsync(receta);
    }

    public async Task<bool> UpdateAsync(int id, Receta receta)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        receta.Id = id;
        await _repository.UpdateAsync(receta);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var receta = await _repository.GetByIdAsync(id);

        if (receta is null)
        {
            return false;
        }

        await _repository.DeleteAsync(receta);

        return true;
    }

    public async Task<IEnumerable<Receta>> GetByDificultadAsync(DificultadReceta dificultad)
    {
        return await _repository.FindAsync(receta => receta.Dificultad == dificultad);
    }
}
