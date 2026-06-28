using Kitch.Application.DTOs.RecetaIa;

namespace Kitch.Application.Interfaces;

public interface IRecetaIaService
{
    // Genera una receta (borrador, sin guardar) usando la alacena del usuario.
    Task<RecetaGeneradaDto> GenerarRecetaAsync(int usuarioId, string? preferencias);

    // Persiste la receta elegida por el usuario (crea ingredientes faltantes,
    // la guarda con sus pasos y la marca como favorita del usuario).
    Task<RecetaGuardadaResponse> GuardarRecetaAsync(int usuarioId, RecetaGeneradaDto receta);
}
