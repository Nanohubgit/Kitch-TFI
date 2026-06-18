using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ContratoSubService : IContratoSubService
{
    private readonly IRepository<ContratoSub> _repository;

    public ContratoSubService(IRepository<ContratoSub> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ContratoSub>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<ContratoSub>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _repository.FindAsync(contratoSub => contratoSub.UsuarioId == usuarioId);
    }

    public async Task<ContratoSub?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<ContratoSub> CreateAsync(ContratoSub contratoSub)
    {
        return await _repository.AddAsync(contratoSub);
    }

    public async Task<bool> UpdateAsync(int id, ContratoSub contratoSub)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        contratoSub.Id = id;
        await _repository.UpdateAsync(contratoSub);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var contratoSub = await _repository.GetByIdAsync(id);

        if (contratoSub is null)
        {
            return false;
        }

        await _repository.DeleteAsync(contratoSub);

        return true;
    }
}
