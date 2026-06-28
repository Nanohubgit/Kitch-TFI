using Kitch.Application.DTOs.Usuarios;

namespace Kitch.Application.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResponseDto>> GetAllAsync();
    Task<UsuarioResponseDto?> GetByIdAsync(int id);
    Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto usuario);
    Task<bool> UpdateAsync(int id, UsuarioUpdateDto usuario);
    Task<bool> CambiarRolAsync(int usuarioId, string nuevoRol);
    Task<bool> DeleteAsync(int id);
}
