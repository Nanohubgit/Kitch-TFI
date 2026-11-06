namespace Kitch.Domain.Constants;

public static class TurnosComida
{
    public const string Desayuno = "Desayuno";
    public const string Almuerzo = "Almuerzo";
    public const string Merienda = "Merienda";
    public const string Cena = "Cena";

    public static readonly IReadOnlyList<string> Validos = [Desayuno, Almuerzo, Merienda, Cena];

    public static string Normalizar(string? turno)
    {
        var limpio = (turno ?? string.Empty).Trim();
        var valido = Validos.FirstOrDefault(opcion => opcion.Equals(limpio, StringComparison.OrdinalIgnoreCase));

        return valido ?? throw new ArgumentException(
            "El turno no es válido. Usá Desayuno, Almuerzo, Merienda o Cena.");
    }
}
