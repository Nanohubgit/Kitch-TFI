using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class SuscripcionService : ISuscripcionService
{
    private readonly IRepository<Suscripcion> _repository;

    public SuscripcionService(IRepository<Suscripcion> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Suscripcion>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Suscripcion?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Suscripcion> CreateAsync(Suscripcion suscripcion)
    {
        return await _repository.AddAsync(suscripcion);
    }

    public async Task<bool> UpdateAsync(int id, Suscripcion suscripcion)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        suscripcion.Id = id;
        await _repository.UpdateAsync(suscripcion);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var suscripcion = await _repository.GetByIdAsync(id);

        if (suscripcion is null)
        {
            return false;
        }

        await _repository.DeleteAsync(suscripcion);

        return true;
    }
}
