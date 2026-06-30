using Kitch.Application.DTOs.Recomendacion;

namespace Kitch.Application.Interfaces;

public interface IRecomendacionService
{
    Task<IEnumerable<RecetaCompatibleDto>> RecomendarAsync(int usuarioId, int? maxFaltantes = null);
}
