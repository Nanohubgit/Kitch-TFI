namespace Kitch.Application.Interfaces;

public interface IRecomendacionIaService
{
    // Analiza el stock real del usuario y las recetas cargadas para que la IA recomiende
    // qué cocinar, priorizando coincidencia de ingredientes e indicando los faltantes.
    Task<string> RecomendarRecetasAsync(int usuarioId, string? preferencias = null);
}
