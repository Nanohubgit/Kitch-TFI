using Kitch.Application.DTOs.Sustituciones;

namespace Kitch.Application.Interfaces;

public interface ISustitucionService
{
    // Devuelve los reemplazos viables para un ingrediente, priorizando los que el
    // usuario ya tiene disponibles en su Alacena Virtual.
    Task<IEnumerable<SustitutoSugerido>> BuscarSustitutosAsync(int usuarioId, int ingredienteId);
}
