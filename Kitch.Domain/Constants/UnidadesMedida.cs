namespace Kitch.Domain.Constants;

public static class UnidadesMedida
{
    public const string Gramo = "g";
    public const string Mililitro = "ml";
    public const string Unidad = "u";

    private static readonly Dictionary<string, (string Base, decimal Factor)> Equivalencias =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["g"] = (Gramo, 1m),
            ["gr"] = (Gramo, 1m),
            ["grs"] = (Gramo, 1m),
            ["gramo"] = (Gramo, 1m),
            ["gramos"] = (Gramo, 1m),
            ["mg"] = (Gramo, 0.001m),
            ["kg"] = (Gramo, 1000m),
            ["kgs"] = (Gramo, 1000m),
            ["kilo"] = (Gramo, 1000m),
            ["kilos"] = (Gramo, 1000m),
            ["kilogramo"] = (Gramo, 1000m),
            ["kilogramos"] = (Gramo, 1000m),
            ["ml"] = (Mililitro, 1m),
            ["mililitro"] = (Mililitro, 1m),
            ["mililitros"] = (Mililitro, 1m),
            ["cc"] = (Mililitro, 1m),
            ["cm3"] = (Mililitro, 1m),
            ["cl"] = (Mililitro, 10m),
            ["l"] = (Mililitro, 1000m),
            ["lt"] = (Mililitro, 1000m),
            ["lts"] = (Mililitro, 1000m),
            ["litro"] = (Mililitro, 1000m),
            ["litros"] = (Mililitro, 1000m),
            ["taza"] = (Mililitro, 250m),
            ["tazas"] = (Mililitro, 250m),
            ["cda"] = (Mililitro, 15m),
            ["cdas"] = (Mililitro, 15m),
            ["cucharada"] = (Mililitro, 15m),
            ["cucharadas"] = (Mililitro, 15m),
            ["cdita"] = (Mililitro, 5m),
            ["cditas"] = (Mililitro, 5m),
            ["cucharadita"] = (Mililitro, 5m),
            ["cucharaditas"] = (Mililitro, 5m),
            ["u"] = (Unidad, 1m),
            ["un"] = (Unidad, 1m),
            ["ud"] = (Unidad, 1m),
            ["uds"] = (Unidad, 1m),
            ["unidad"] = (Unidad, 1m),
            ["unidades"] = (Unidad, 1m),
            ["pieza"] = (Unidad, 1m),
            ["piezas"] = (Unidad, 1m)
        };

    public static string Clave(string? unidad)
    {
        var limpia = (unidad ?? string.Empty).Trim().TrimEnd('.').ToLowerInvariant();
        return limpia.Length == 0 ? Unidad : limpia;
    }

    public static string Dimension(string? unidad)
    {
        var clave = Clave(unidad);
        return Equivalencias.TryGetValue(clave, out var equivalencia) ? equivalencia.Base : clave;
    }

    public static bool SonCompatibles(string? unidadA, string? unidadB) =>
        Dimension(unidadA) == Dimension(unidadB);

    public static bool TryConvertir(decimal cantidad, string? desde, string? hacia, out decimal resultado)
    {
        var origen = Clave(desde);
        var destino = Clave(hacia);

        if (origen == destino)
        {
            resultado = cantidad;
            return true;
        }

        if (Equivalencias.TryGetValue(origen, out var a) &&
            Equivalencias.TryGetValue(destino, out var b) &&
            a.Base == b.Base)
        {
            resultado = Math.Round(cantidad * a.Factor / b.Factor, 4);
            return true;
        }

        resultado = 0m;
        return false;
    }
}
