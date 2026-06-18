using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class PagoService : IPagoService
{
    private readonly IRepository<Pago> _repository;

    public PagoService(IRepository<Pago> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Pago>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Pago>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _repository.FindAsync(pago => pago.UsuarioId == usuarioId);
    }

    public async Task<Pago?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Pago> CreateAsync(Pago pago)
    {
        return await _repository.AddAsync(pago);
    }

    public async Task<bool> UpdateAsync(int id, Pago pago)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        pago.Id = id;
        await _repository.UpdateAsync(pago);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var pago = await _repository.GetByIdAsync(id);

        if (pago is null)
        {
            return false;
        }

        await _repository.DeleteAsync(pago);

        return true;
    }
}
