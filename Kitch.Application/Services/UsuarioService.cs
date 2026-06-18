using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IRepository<Usuario> _repository;

    public UsuarioService(IRepository<Usuario> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        return await _repository.AddAsync(usuario);
    }

    public async Task<bool> UpdateAsync(int id, Usuario usuario)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        usuario.Id = id;
        await _repository.UpdateAsync(usuario);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id);

        if (usuario is null)
        {
            return false;
        }

        await _repository.DeleteAsync(usuario);

        return true;
    }
}
