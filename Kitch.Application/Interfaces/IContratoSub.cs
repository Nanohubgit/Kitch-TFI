using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface IContratoSubService
{
    Task<IEnumerable<ContratoSub>> GetAllAsync();
    Task<IEnumerable<ContratoSub>> GetByUsuarioIdAsync(int usuarioId);
    Task<ContratoSub?> GetByIdAsync(int id);
    Task<ContratoSub> CreateAsync(ContratoSub contratoSub);
    Task<bool> UpdateAsync(int id, ContratoSub contratoSub);
    Task<bool> DeleteAsync(int id);
}
