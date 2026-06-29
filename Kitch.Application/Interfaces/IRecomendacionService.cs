using Kitch.Application.DTOs.Recomendacion;

namespace Kitch.Application.Interfaces;

public interface IRecomendacionService
{
    // Recomienda recetas ordenadas por porcentaje de coincidencia con la alacena del usuario.
    // maxFaltantes (opcional): solo devuelve recetas a las que les falten como máximo N ingredientes.
    Task<IEnumerable<RecetaCompatibleDto>> RecomendarAsync(int usuarioId, int? maxFaltantes = null);
}
