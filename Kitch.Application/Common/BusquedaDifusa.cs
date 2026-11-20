using System.Globalization;
using System.Text;

namespace Kitch.Application.Common;

public static class BusquedaDifusa
{
    private const double UmbralMinimo = 0.68;

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var formD = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);

        foreach (var caracter in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(caracter) ? caracter : ' ');
        }

        var limpio = builder.ToString().Normalize(NormalizationForm.FormC);
        var palabras = limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', palabras);
    }

    public static string EsqueletoPalabra(string? palabra)
    {
        var limpia = Normalizar(palabra);
        if (limpia.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(limpia.Length);
        var anterior = '\0';
        foreach (var caracter in limpia)
        {
            if (caracter == anterior)
            {
                continue;
            }

            builder.Append(caracter);
            anterior = caracter;
        }

        var colapsada = builder.ToString();
        return SingularizarSimple(colapsada);
    }

    public static double Puntaje(string? candidato, string? busqueda)
    {
        var candNorm = Normalizar(candidato);
        var busqNorm = Normalizar(busqueda);
        if (candNorm.Length == 0 || busqNorm.Length == 0)
        {
            return 0;
        }

        if (candNorm == busqNorm)
        {
            return 1.0;
        }

        var candPalabras = candNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var busqPalabras = busqNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var candEsqueletos = candPalabras.Select(EsqueletoPalabra).ToArray();
        var busqEsqueletos = busqPalabras.Select(EsqueletoPalabra).ToArray();

        var candEsqueletoCompleto = string.Join(' ', candEsqueletos);
        var busqEsqueletoCompleto = string.Join(' ', busqEsqueletos);

        if (candEsqueletoCompleto == busqEsqueletoCompleto)
        {
            return 0.96;
        }

        if (candNorm.Contains(busqNorm, StringComparison.Ordinal) ||
            candEsqueletoCompleto.Contains(busqEsqueletoCompleto, StringComparison.Ordinal))
        {
            var proporcion = (double)Math.Min(busqEsqueletoCompleto.Length, candEsqueletoCompleto.Length) /
                             Math.Max(busqEsqueletoCompleto.Length, candEsqueletoCompleto.Length);
            return 0.84 + 0.10 * proporcion;
        }

        if (busqEsqueletos.Length == 1 && candEsqueletos.Length == 1)
        {
            return SimilitudPalabra(candEsqueletos[0], busqEsqueletos[0]);
        }

        var suma = 0.0;
        var coincidencias = 0;
        foreach (var palabraBusq in busqEsqueletos)
        {
            if (palabraBusq.Length <= 2 && busqEsqueletos.Length > 1)
            {
                continue;
            }

            var mejorPalabra = candEsqueletos.Max(palabraCand => SimilitudPalabra(palabraCand, palabraBusq));
            if (mejorPalabra >= 0.72)
            {
                coincidencias++;
                suma += mejorPalabra;
            }
        }

        var relevantesBusq = busqEsqueletos.Count(palabra => palabra.Length > 2 || busqEsqueletos.Length == 1);
        var relevantesCand = candEsqueletos.Count(palabra => palabra.Length > 2 || candEsqueletos.Length == 1);
        if (relevantesBusq == 0 || coincidencias == 0)
        {
            return 0;
        }

        var coberturaBusq = (double)coincidencias / relevantesBusq;
        if (coberturaBusq < 0.65)
        {
            return 0;
        }

        var coberturaCand = relevantesCand > 0 ? (double)coincidencias / relevantesCand : 0;
        var promedioPalabra = suma / coincidencias;
        return Math.Min(0.95, promedioPalabra * (0.72 * coberturaBusq + 0.28 * coberturaCand));
    }

    public static bool Coincide(string? candidato, string? busqueda) =>
        Puntaje(candidato, busqueda) >= UmbralMinimo;

    public static List<T> BuscarMejores<T>(IEnumerable<T> items, Func<T, string> selector, string? busqueda)
    {
        if (string.IsNullOrWhiteSpace(busqueda))
        {
            return [];
        }

        var evaluados = items
            .Select(item => (Item: item, Score: Puntaje(selector(item), busqueda)))
            .Where(par => par.Score >= UmbralMinimo)
            .OrderByDescending(par => par.Score)
            .ToList();

        if (evaluados.Count == 0)
        {
            return [];
        }

        var mejor = evaluados[0].Score;
        if (mejor >= 0.94)
        {
            return evaluados
                .Where(par => par.Score >= mejor - 0.03)
                .Select(par => par.Item)
                .ToList();
        }

        return evaluados
            .Where(par => par.Score >= mejor - 0.06)
            .Select(par => par.Item)
            .ToList();
    }

    public static T? BuscarMejor<T>(IEnumerable<T> items, Func<T, string> selector, string? busqueda) =>
        BuscarMejores(items, selector, busqueda).FirstOrDefault();

    private static double SimilitudPalabra(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        if (a == b)
        {
            return 1.0;
        }

        if (a.Length >= 4 && b.Length >= 4 && (a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal)))
        {
            var ratio = (double)Math.Min(a.Length, b.Length) / Math.Max(a.Length, b.Length);
            if (ratio >= 0.72)
            {
                return 0.85 * ratio + 0.10;
            }
        }

        var distancia = DistanciaLevenshtein(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        var maxPermitida = maxLen <= 4 ? 1 : maxLen <= 8 ? 2 : 3;
        if (distancia > maxPermitida)
        {
            return 0;
        }

        return 1.0 - (double)distancia / maxLen;
    }

    private static string SingularizarSimple(string palabra)
    {
        if (palabra.Length <= 3)
        {
            return palabra;
        }

        if (palabra.EndsWith("ces", StringComparison.Ordinal) && palabra.Length > 4)
        {
            return palabra[..^3] + "z";
        }

        if (palabra.EndsWith("es", StringComparison.Ordinal) && palabra.Length > 4 &&
            "nlrdj".Contains(palabra[^3]))
        {
            return palabra[..^2];
        }

        if (palabra.EndsWith('s') && !palabra.EndsWith("us", StringComparison.Ordinal))
        {
            return palabra[..^1];
        }

        return palabra;
    }

    private static int DistanciaLevenshtein(string a, string b)
    {
        var filas = a.Length + 1;
        var columnas = b.Length + 1;
        var matriz = new int[filas, columnas];

        for (var i = 0; i < filas; i++)
        {
            matriz[i, 0] = i;
        }

        for (var j = 0; j < columnas; j++)
        {
            matriz[0, j] = j;
        }

        for (var i = 1; i < filas; i++)
        {
            for (var j = 1; j < columnas; j++)
            {
                var costo = a[i - 1] == b[j - 1] ? 0 : 1;
                var eliminar = matriz[i - 1, j] + 1;
                var insertar = matriz[i, j - 1] + 1;
                var sustituir = matriz[i - 1, j - 1] + costo;
                matriz[i, j] = Math.Min(Math.Min(eliminar, insertar), sustituir);
            }
        }

        return matriz[a.Length, b.Length];
    }
}
