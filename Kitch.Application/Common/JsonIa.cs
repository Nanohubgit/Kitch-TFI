using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kitch.Application.Common;

public static partial class JsonIa
{
    public static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new DecimalFlexibleConverter(), new EnteroFlexibleConverter() }
    };

    public static string LimpiarBloqueCodigo(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        var limpio = json.Trim();
        if (limpio.StartsWith("```"))
        {
            limpio = limpio.Trim('`');
            var saltoLinea = limpio.IndexOf('\n');
            if (saltoLinea >= 0 && limpio[..saltoLinea].Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                limpio = limpio[(saltoLinea + 1)..];
            }
        }

        var inicio = limpio.IndexOf('{');
        var fin = limpio.LastIndexOf('}');
        return inicio >= 0 && fin > inicio ? limpio[inicio..(fin + 1)] : limpio;
    }

    public static decimal ParsearCantidad(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return 0m;
        }

        var valor = texto.Trim().Replace(',', '.');

        var mixta = FraccionMixta().Match(valor);
        if (mixta.Success)
        {
            var entero = decimal.Parse(mixta.Groups[1].Value, CultureInfo.InvariantCulture);
            var numerador = decimal.Parse(mixta.Groups[2].Value, CultureInfo.InvariantCulture);
            var denominador = decimal.Parse(mixta.Groups[3].Value, CultureInfo.InvariantCulture);
            return denominador == 0 ? entero : entero + numerador / denominador;
        }

        var fraccion = Fraccion().Match(valor);
        if (fraccion.Success)
        {
            var numerador = decimal.Parse(fraccion.Groups[1].Value, CultureInfo.InvariantCulture);
            var denominador = decimal.Parse(fraccion.Groups[2].Value, CultureInfo.InvariantCulture);
            return denominador == 0 ? 0m : numerador / denominador;
        }

        var numero = Numero().Match(valor);
        return numero.Success &&
               decimal.TryParse(numero.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var resultado)
            ? resultado
            : 0m;
    }

    [GeneratedRegex(@"^(\d+)\s+(\d+)\s*/\s*(\d+)")]
    private static partial Regex FraccionMixta();

    [GeneratedRegex(@"^(\d+)\s*/\s*(\d+)")]
    private static partial Regex Fraccion();

    [GeneratedRegex(@"\d+(\.\d+)?")]
    private static partial Regex Numero();

    private sealed class DecimalFlexibleConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Number => reader.TryGetDecimal(out var valor) ? valor : (decimal)reader.GetDouble(),
                JsonTokenType.String => ParsearCantidad(reader.GetString()),
                JsonTokenType.True => 1m,
                _ => SaltarYDevolverCero(ref reader)
            };

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);
    }

    private sealed class EnteroFlexibleConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var valor = reader.TokenType switch
            {
                JsonTokenType.Number => reader.TryGetDecimal(out var numero) ? numero : (decimal)reader.GetDouble(),
                JsonTokenType.String => ParsearCantidad(reader.GetString()),
                _ => SaltarYDevolverCero(ref reader)
            };

            return valor >= int.MaxValue ? int.MaxValue : valor <= int.MinValue ? int.MinValue : (int)Math.Round(valor);
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);
    }

    private static decimal SaltarYDevolverCero(ref Utf8JsonReader reader)
    {
        reader.Skip();
        return 0m;
    }
}
