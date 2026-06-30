using Kitch.Application.DTOs.Sustituciones;

namespace Kitch.Application.Interfaces;

public interface ISustitucionService
{
    Task<IEnumerable<SustitutoSugerido>> BuscarSustitutosAsync(int usuarioId, int ingredienteId);
}
