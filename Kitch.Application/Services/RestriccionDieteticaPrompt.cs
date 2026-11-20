using System.Globalization;
using System.Text;

namespace Kitch.Application.Services;

internal static class RestriccionDieteticaPrompt
{
    public static string ParaSystemPrompt(string? preferencia)
    {
        if (string.IsNullOrWhiteSpace(preferencia) || EsNinguna(preferencia))
        {
            return "El usuario no informó una restricción dietética específica (preferencia: Ninguna). " +
                "No asumas alergias, pero si un plato se basa en frutos secos o mariscos, mencionálo con claridad.";
        }

        var original = preferencia.Trim();
        var clave = QuitarTildes(original);

        if (EsCeliaco(clave))
        {
            return $"El usuario es Celíaco (preferencia: {original}). " +
                "BAJO NINGÚN CONCEPTO sugieras recetas ni ingredientes con TACC/Gluten " +
                "(trigo, avena no certificada sin TACC, cebada, centeno, pan común, fideos de trigo, " +
                "salsas con harina de trigo, rebozados, cerveza de cebada, etc.). " +
                "Usá únicamente alternativas sin TACC.";
        }

        if (clave.Contains("vegan", StringComparison.Ordinal))
        {
            return $"El usuario es Vegano (preferencia: {original}). " +
                "BAJO NINGÚN CONCEPTO sugieras carne, pescado, mariscos, huevo, lácteos, miel ni derivados animales.";
        }

        if (clave.Contains("vegetar", StringComparison.Ordinal))
        {
            return $"El usuario es Vegetariano (preferencia: {original}). " +
                "No sugieras carne ni pescado. Huevo y lácteos están permitidos salvo que indique lo contrario.";
        }

        if (clave.Contains("diabet", StringComparison.Ordinal))
        {
            return $"El usuario es Diabético (preferencia: {original}). " +
                "No sugieras azúcar, miel, dulce de leche, mermelada, chocolate, gaseosa ni jarabes. " +
                "Si pide algo dulce, torta o postre, tiene que ser un postre real sin azúcar " +
                "(fruta al horno, compota sin azúcar, flan de huevo sin azúcar, avena con banana). " +
                "PROHIBIDO disfrazar la alacena de postre: nada de galletas, budines o tortas de lentejas, " +
                "garbanzos, arroz, polenta, papa o carne. Si en la alacena no hay fruta ni huevo, " +
                "igual proponé el postre real y marcá esos ingredientes para comprar.";
        }

        return $"El usuario indicó esta restricción o alergia dietética: {original}. " +
            "Respetala en TODA receta, ingrediente, sustitución y recomendación. " +
            "Si un plato la viola, no lo ofrezcas.";
    }

    private static bool EsNinguna(string preferencia) =>
        QuitarTildes(preferencia) is "ninguna" or "ninguno" or "ningun" or "sin restricciones";

    private static bool EsCeliaco(string clave) =>
        clave.Contains("celiac", StringComparison.Ordinal) ||
        clave.Contains("tacc", StringComparison.Ordinal) ||
        clave.Contains("gluten", StringComparison.Ordinal) ||
        clave.Contains("sintacc", StringComparison.Ordinal);

    private static string QuitarTildes(string texto)
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
