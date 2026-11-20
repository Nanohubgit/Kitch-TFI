namespace Kitch.Application.Interfaces;

public interface IIngredienteValidadorService
{
    Task AsegurarEsAlimentoAsync(string nombre);
}
