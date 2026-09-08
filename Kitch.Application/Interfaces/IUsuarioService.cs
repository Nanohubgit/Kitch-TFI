using Kitch.Application.DTOs.Usuarios;

namespace Kitch.Application.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(int solicitanteId);
    Task<UsuarioResponseDto?> GetByIdAsync(int id);
    Task<UsuarioResponseDto?> GetByIdAdminAsync(int id, int solicitanteId);
    Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto usuario, int solicitanteId);
    Task<bool> UpdateAsync(int id, UsuarioUpdateDto usuario, int solicitanteId);
    Task<bool> ActualizarPerfilAsync(int usuarioId, ActualizarPerfilDto perfil);
    Task<bool> CambiarRolAsync(int usuarioId, string nuevoRol, int adminEjecutorId);
    Task<bool> DeleteAsync(int id, int adminEjecutorId);
}
