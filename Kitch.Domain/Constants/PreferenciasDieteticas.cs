namespace Kitch.Domain.Constants;

public static class PreferenciasDieteticas
{
    public const string Ninguna = "Ninguna";
    public const string Vegetariano = "Vegetariano";
    public const string Vegano = "Vegano";
    public const string Celiaco = "Celiaco";
    public const string Diabetico = "Diabetico";

    public static readonly IReadOnlyList<string> Validas =
        [Ninguna, Vegetariano, Vegano, Celiaco, Diabetico];
}
