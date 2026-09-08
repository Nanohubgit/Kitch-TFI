using System.Globalization;
using System.Text;
using Kitch.Application.Interfaces;

namespace Kitch.Application.Services;

public class IngredienteNormalizerService : IIngredienteNormalizerService
{
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
        "pastas"
    };

    public string Normalizar(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del ingrediente no puede ser nulo o vacío.", nameof(nombre));
        }

        var texto = QuitarTildes(nombre.Trim().ToLowerInvariant());
        var palabras = texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < palabras.Length; i++)
        {
            palabras[i] = Singularizar(palabras[i]);
        }

        var normalizado = string.Join(' ', palabras);
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new ArgumentException("El nombre del ingrediente no puede ser nulo o vacío.", nameof(nombre));
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

        if (palabra.EndsWith("ces", StringComparison.Ordinal) && palabra.Length > 4)
        {
            return palabra[..^3] + "z";
        }

        if (palabra.EndsWith('s') && !palabra.EndsWith("ss", StringComparison.Ordinal))
        {
            return palabra[..^1];
        }

        return palabra;
    }
}
