using System.Globalization;
using System.Text;

namespace Kitch.Domain.Constants;

public static class PedidoRecetaPremium
{
    private static readonly HashSet<string> Negaciones = new(StringComparer.Ordinal)
    {
        "no", "nada", "ni", "sin", "poco", "menos", "nunca", "tampoco", "evitar", "evita"
    };

    private static readonly HashSet<string> ContextoProfesional = new(StringComparer.Ordinal)
    {
        "receta", "recetas", "nivel", "plato", "platos", "chef", "cocina", "comida", "menu", "estilo", "tecnica"
    };

    private const int VentanaNegacion = 3;
    private const int VentanaContexto = 3;

    public static bool EsPedido(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var palabras = Tokenizar(texto);

        for (var i = 0; i < palabras.Length; i++)
        {
            var palabra = palabras[i];
            var esDificultad = palabra.StartsWith("dificil", StringComparison.Ordinal) ||
                               palabra.StartsWith("avanzad", StringComparison.Ordinal) ||
                               palabra.StartsWith("complej", StringComparison.Ordinal);
            var esProfesional = palabra.StartsWith("profesional", StringComparison.Ordinal) &&
                                TieneContextoProfesional(palabras, i);

            if ((esDificultad || esProfesional) && !EstaNegada(palabras, i))
            {
                return true;
            }
        }

        return false;
    }

    public static int TopeDiario(string? rol) =>
        RolUsuario.TieneAccesoPremium(rol)
            ? LimitesPlan.MaxPeticionesIaProfesional
            : LimitesPlan.MaxPeticionesIaBasico;

    public static string MensajeTopeDiario(string? rol) =>
        RolUsuario.TieneAccesoPremium(rol)
            ? LimitesPlan.MensajeLimiteIaProfesional
            : LimitesPlan.MensajeLimiteIaBasico;

    private static bool EstaNegada(string[] palabras, int indice)
    {
        for (var j = Math.Max(0, indice - VentanaNegacion); j < indice; j++)
        {
            if (Negaciones.Contains(palabras[j]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TieneContextoProfesional(string[] palabras, int indice)
    {
        var desde = Math.Max(0, indice - VentanaContexto);
        var hasta = Math.Min(palabras.Length - 1, indice + VentanaContexto);

        for (var j = desde; j <= hasta; j++)
        {
            if (j != indice && ContextoProfesional.Contains(palabras[j]))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] Tokenizar(string texto)
    {
        var formD = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);

        foreach (var caracter in formD)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
            if (categoria == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(caracter) ? caracter : ' ');
        }

        return builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
