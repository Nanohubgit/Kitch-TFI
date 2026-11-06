using System.Globalization;
using System.Text;

namespace Kitch.Domain.Constants;

public static class RestriccionDietetica
{
    private static readonly string[] Carnes =
    [
        "carne", "pollo", "cerdo", "vaca", "ternera", "jamon", "panceta", "tocino", "bacon", "chorizo",
        "salchicha", "morcilla", "bondiola", "bife", "lomo", "matambre", "vacio", "asado", "costilla",
        "cordero", "pavo", "pato", "conejo", "salame", "mortadela", "hamburguesa", "pechuga", "muslo",
        "gelatina", "caldo de carne", "caldo de pollo"
    ];

    private static readonly string[] Pescados =
    [
        "pescado", "atun", "salmon", "merluza", "sardina", "anchoa", "caballa", "trucha", "bacalao",
        "camaron", "langostino", "marisco", "calamar", "pulpo", "mejillon", "almeja", "cangrejo"
    ];

    private static readonly string[] DerivadosAnimales =
    [
        "huevo", "clara", "yema", "leche", "manteca", "mantequilla", "queso", "crema", "yogur", "yogurt",
        "ricota", "mozzarella", "muzzarella", "parmesano", "dulce de leche", "miel", "mayonesa", "nata"
    ];

    private static readonly string[] AlternativasVeganas =
    [
        "vegan", "vegetal", "de coco", "de almendra", "de soja", "de avena", "de arroz", "de mani"
    ];

    private static readonly string[] ConGluten =
    [
        "harina", "trigo", "pan", "pan rallado", "fideo", "fideos", "pasta", "pastas", "cebada", "centeno",
        "avena", "cerveza", "galleta", "galletita", "rebozador", "semola", "cuscus", "couscous", "seitan",
        "tapa de empanada", "masa", "bizcochuelo", "vainilla", "tostada", "ravioles", "noquis", "lasagna"
    ];

    private static readonly string[] AlternativasSinGluten =
    [
        "sin tacc", "sin gluten", "de arroz", "de maiz", "de garbanzo", "de almendra", "de mandioca", "de papa",
        "de coco", "de quinoa", "de trigo sarraceno"
    ];

    private static readonly string[] Lacteos =
    [
        "leche", "manteca", "mantequilla", "queso", "crema", "yogur", "yogurt", "ricota", "mozzarella",
        "muzzarella", "parmesano", "dulce de leche", "nata"
    ];

    private static readonly string[] FrutosSecos =
    [
        "nuez", "almendra", "avellana", "castana", "pistacho", "mani", "anacardo", "caju", "pecan"
    ];

    private static readonly HashSet<string> PalabrasVacias = new(StringComparer.Ordinal)
    {
        "alergia", "alergico", "alergica", "al", "a", "la", "el", "los", "las", "de", "del", "sin", "intolerante",
        "intolerancia", "no", "como", "con", "y", "o", "dieta", "soy", "tengo", "restriccion", "ninguna"
    };

    public static bool PermiteIngrediente(string? preferencia, string? nombreIngrediente)
    {
        if (string.IsNullOrWhiteSpace(nombreIngrediente) || string.IsNullOrWhiteSpace(preferencia))
        {
            return true;
        }

        var clave = Normalizar(preferencia);
        if (clave is "ninguna" or "ninguno" or "ningun" or "sin restricciones")
        {
            return true;
        }

        var nombre = $" {Normalizar(nombreIngrediente)} ";

        if (clave.Contains("vegan", StringComparison.Ordinal))
        {
            return ContieneAlguno(nombre, AlternativasVeganas) ||
                !ContieneAlguno(nombre, Carnes, Pescados, DerivadosAnimales);
        }

        if (clave.Contains("vegetar", StringComparison.Ordinal))
        {
            return !ContieneAlguno(nombre, Carnes, Pescados);
        }

        if (clave.Contains("celiac", StringComparison.Ordinal) ||
            clave.Contains("tacc", StringComparison.Ordinal) ||
            clave.Contains("gluten", StringComparison.Ordinal))
        {
            return ContieneAlguno(nombre, AlternativasSinGluten) || !ContieneAlguno(nombre, ConGluten);
        }

        var prohibidos = new List<string>();
        if (clave.Contains("lactosa", StringComparison.Ordinal) || clave.Contains("lacteo", StringComparison.Ordinal))
        {
            prohibidos.AddRange(Lacteos);
        }

        if (clave.Contains("fruto", StringComparison.Ordinal) && clave.Contains("seco", StringComparison.Ordinal))
        {
            prohibidos.AddRange(FrutosSecos);
        }

        if (clave.Contains("marisco", StringComparison.Ordinal) || clave.Contains("pescado", StringComparison.Ordinal))
        {
            prohibidos.AddRange(Pescados);
        }

        prohibidos.AddRange(clave
            .Split([' ', ',', ';', '/', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Where(palabra => palabra.Length >= 3 && !PalabrasVacias.Contains(palabra))
            .Select(palabra => palabra.Length > 3 && palabra.EndsWith('s') ? palabra[..^1] : palabra));

        if (prohibidos.Count == 0)
        {
            return true;
        }

        if (prohibidos.Any(termino => Lacteos.Contains(termino)) && ContieneAlguno(nombre, AlternativasVeganas))
        {
            return true;
        }

        return !ContieneAlguno(nombre, [.. prohibidos]);
    }

    private static bool ContieneAlguno(string nombreConEspacios, params string[][] listas) =>
        listas.Any(lista => lista.Any(termino =>
            nombreConEspacios.Contains($" {termino} ", StringComparison.Ordinal) ||
            nombreConEspacios.Contains($" {termino}s ", StringComparison.Ordinal) ||
            nombreConEspacios.Contains($" {termino}es ", StringComparison.Ordinal)));

    private static string Normalizar(string texto)
    {
        var formD = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);

        foreach (var caracter in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(caracter);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
