using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Kitch.Application.Interfaces;

namespace Kitch.Application.Services;

public class IngredienteNormalizerService : IIngredienteNormalizerService
{
    private static readonly Regex RegexParentesisCalificador = new(
        @"\s*[\(\[]\s*[^)\]]*(?:opcional|optativ[oa]?|a\s+gusto|al\s+gusto|si\s+se\s+desea|si\s+quer[eé]s|si\s+prefier\w*|para\s+(?:decorar|servir|acompa[nñ]ar|terminar|espolvorear)|c/n|cantidad\s+necesaria|aprox\.?)[^)\]]*[\)\]]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RegexSufijoCalificador = new(
        @"(?:\s*[,;/\-–—]\s*|\s+)(?:opcional|optativ[oa]?|a\s+gusto|al\s+gusto|si\s+se\s+desea|c/n|cantidad\s+necesaria)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RegexParentesisVacios = new(
        @"\s*[\(\[]\s*[\)\]]",
        RegexOptions.Compiled);

    private static readonly Regex RegexEspaciosMultiples = new(
        @"\s+",
        RegexOptions.Compiled);

    private static readonly HashSet<string> PluralesInvariantes = new(StringComparer.Ordinal)
    {
        "lentejas",
        "alubias",
        "habas",
        "fideos",
        "noquis",
        "nachos",
        "ravioles",
        "especias",
        "pastas",
        "anis",
        "ananas",
        "cous",
        "couscous",
        "hummus",
        "humus"
    };

    public string LimpiarNombreVisible(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return string.Empty;
        }

        var limpio = nombre.Trim();
        string anterior;
        do
        {
            anterior = limpio;
            limpio = RegexParentesisCalificador.Replace(limpio, " ");
            limpio = RegexSufijoCalificador.Replace(limpio, string.Empty);
            limpio = RegexParentesisVacios.Replace(limpio, " ");
            limpio = RegexEspaciosMultiples.Replace(limpio, " ").Trim(' ', ',', ';', '-', '/', '–', '—');
        }
        while (!string.Equals(limpio, anterior, StringComparison.Ordinal));

        return limpio;
    }

    public string Normalizar(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del ingrediente no puede ser nulo o vacío.");
        }

        var visible = LimpiarNombreVisible(nombre);
        var texto = QuitarTildes(visible.ToLowerInvariant());
        var palabras = texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < palabras.Length; i++)
        {
            palabras[i] = Singularizar(palabras[i]);
        }

        var normalizado = string.Join(' ', palabras);
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new ArgumentException("El nombre del ingrediente no puede ser nulo o vacío.");
        }

        return normalizado;
    }

    private static string QuitarTildes(string texto)
    {
        var formD = texto.Normalize(NormalizationForm.FormD);
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

    private static string Singularizar(string palabra)
    {
        if (PluralesInvariantes.Contains(palabra) || palabra.Length <= 3)
        {
            return palabra;
        }

        if (palabra.EndsWith("ces", StringComparison.Ordinal) && palabra.Length > 4 && "aeiou".Contains(palabra[^4]))
        {
            return palabra[..^3] + "z";
        }

        if (palabra.EndsWith("es", StringComparison.Ordinal) && palabra.Length > 4)
        {
            var raiz = palabra[..^2];
            var ultima = raiz[^1];
            var anterior = raiz[^2];
            if ("nlrdj".Contains(ultima) && "aeiou".Contains(anterior))
            {
                return raiz;
            }
        }

        if (palabra.EndsWith('s') && !palabra.EndsWith("ss", StringComparison.Ordinal) &&
            !palabra.EndsWith("us", StringComparison.Ordinal))
        {
            return palabra[..^1];
        }

        return palabra;
    }
}
