using System.Globalization;
using System.Text;

namespace Kitch.Domain.Constants;

public static class IngredientesBasicos
{
    private static readonly HashSet<string> VariantesAgua = new(StringComparer.Ordinal)
    {
        "agua",
        "agua fria",
        "agua tibia",
        "agua caliente",
        "agua hirviendo",
        "agua potable",
        "agua natural",
        "agua filtrada",
        "agua comun",
        "agua de la canilla",
        "agua del grifo",
        "agua para hervir",
        "agua para coccion"
    };

    public static bool EsAgua(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return false;
        }

        var limpio = NormalizarBasico(nombre);
        return VariantesAgua.Contains(limpio);
    }

    private static string NormalizarBasico(string texto)
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

        var sinTildes = builder.ToString().Normalize(NormalizationForm.FormC);
        var palabras = sinTildes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', palabras);
    }
}
