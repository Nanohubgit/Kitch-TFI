using Kitch.Domain.Entities;

namespace Kitch.Domain.Constants;

public static class ComplejidadReceta
{
    public const int MaxPasosNoDificil = 10;
    public const int MaxIngredientesNoDificil = 12;
    public const int MaxMinutosNoDificil = 120;

    public const int PasosSugeridosBasico = 8;
    public const int IngredientesSugeridosBasico = 10;
    public const int MinutosSugeridosBasico = 90;

    private static readonly string[] TecnicasAvanzadas =
    [
        "sous vide",
        "sous-vide",
        "al vacio",
        "al vacío",
        "templar chocolate",
        "templado de chocolate",
        "temperar chocolate",
        "esferificacion",
        "esferificación",
        "flambear",
        "flambeado",
        "masa madre",
        "laminado",
        "hojaldre casero",
        "confitar",
        "confitado en grasa",
        "nitrogeno liquido",
        "nitrógeno líquido",
        "gelificacion",
        "gelificación",
        "ahumado en frio",
        "ahumado en frío",
        "macarons",
        "souffle",
        "soufflé",
        "croissant",
        "consomé clarificado",
        "consome clarificado",
        "fondo oscuro",
        "demi-glace",
        "demi glace"
    ];

    public static bool EsDificilPorEstructura(
        int tiempoPreparacionMinutos,
        int cantidadPasos,
        int cantidadIngredientes,
        IEnumerable<string?> textos)
    {
        if (tiempoPreparacionMinutos > MaxMinutosNoDificil ||
            cantidadPasos > MaxPasosNoDificil ||
            cantidadIngredientes > MaxIngredientesNoDificil)
        {
            return true;
        }

        return textos
            .Where(texto => !string.IsNullOrWhiteSpace(texto))
            .Select(texto => texto!.ToLowerInvariant())
            .Any(texto => TecnicasAvanzadas.Any(tecnica => texto.Contains(tecnica, StringComparison.Ordinal)));
    }

    public static DificultadReceta Efectiva(
        DificultadReceta declarada,
        int tiempoPreparacionMinutos,
        int cantidadPasos,
        int cantidadIngredientes,
        IEnumerable<string?> textos) =>
        EsDificilPorEstructura(tiempoPreparacionMinutos, cantidadPasos, cantidadIngredientes, textos)
            ? DificultadReceta.Dificil
            : declarada;

    public static DificultadReceta Parsear(string? dificultad) =>
        Enum.TryParse<DificultadReceta>(dificultad?.Trim(), ignoreCase: true, out var resultado)
            ? resultado
            : DificultadReceta.Medio;
}
