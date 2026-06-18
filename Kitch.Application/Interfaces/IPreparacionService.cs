namespace Kitch.Application.Interfaces;

public interface IPreparacionService
{
    // usuarioId es necesario para ubicar el stock del usuario que cocina la receta.
    Task DescontarIngredientesAsync(int usuarioId, int recetaId, int porciones);
}
