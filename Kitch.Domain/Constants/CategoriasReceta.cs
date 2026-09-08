namespace Kitch.Domain.Constants;

public static class CategoriasReceta
{
    public const string ValorPorDefecto = "general";

    public static readonly IReadOnlyList<string> Permitidas =
    [
        "pastas",
        "carnes",
        "pollo",
        "ensaladas",
        "sopas",
        "pescados",
        "pizzas",
        "postres",
        "tartas",
        "guisos",
        "general"
    ];

    private static readonly HashSet<string> PermitidasSet = new(Permitidas, StringComparer.Ordinal);

    public static string Normalizar(string? categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
        {
            return ValorPorDefecto;
        }

        var clave = categoria.Trim().ToLowerInvariant();
        return PermitidasSet.Contains(clave) ? clave : ValorPorDefecto;
    }
}
