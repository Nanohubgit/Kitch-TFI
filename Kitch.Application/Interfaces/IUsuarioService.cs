using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario> CreateAsync(Usuario usuario);
    Task<bool> UpdateAsync(int id, Usuario usuario);
    Task<bool> DeleteAsync(int id);
}
