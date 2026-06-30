using Kitch.Application.DTOs.RecetaIa;

namespace Kitch.Application.Interfaces;

public interface IRecetaIaService
{
    Task<RecetaGeneradaDto> GenerarRecetaAsync(int usuarioId, string? preferencias);

    Task<RecetaGuardadaResponse> GuardarRecetaAsync(int usuarioId, RecetaGeneradaDto receta);

    Task AsegurarIngredientesEnCatalogoAsync(RecetaGeneradaDto receta);
}
