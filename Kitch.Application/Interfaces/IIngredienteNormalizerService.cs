namespace Kitch.Application.Interfaces;

public interface IIngredienteNormalizerService
{
    string Normalizar(string? nombre);
    string LimpiarNombreVisible(string? nombre);
}
