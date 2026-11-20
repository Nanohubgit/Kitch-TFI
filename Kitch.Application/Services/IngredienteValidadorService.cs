using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Kitch.Application.Common;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Kitch.Application.Services;

public class IngredienteValidadorService : IIngredienteValidadorService
{
    private const int MinLongitud = 2;
    private const int MaxLongitud = 60;
    private const int MaxPalabras = 6;

    private const string MensajeInvalido =
        "Solo podés agregar alimentos, ingredientes o productos relacionados con comida y recetas.";

    private const string InstruccionValidacion =
        "Sos un validador estricto de alimentos e ingredientes de cocina para una app de recetas (español/Argentina). " +
        "Determiná si el texto corresponde exclusivamente a un alimento, ingrediente culinario, bebida, condimento o " +
        "producto comestible usado para comer o cocinar. " +
        "Rechazá vehículos, objetos, electrónica, ropa, muebles, herramientas, artículos de limpieza, medicamentos, " +
        "personas, lugares, frases sin sentido o cualquier elemento que no sea comida o ingrediente de receta. " +
        "Respondé ÚNICAMENTE con JSON válido, sin markdown: {\"esAlimento\": true} o {\"esAlimento\": false}.";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = JsonIa.Opciones;

    private static readonly Regex RegexAnio = new(@"\b(19\d{2}|20\d{2})\b", RegexOptions.Compiled);
    private static readonly Regex RegexCaracteresInvalidos = new(@"[@#$%^&*={}\[\]<>\\|~_]|https?://|www\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RegexNumeroSuelto = new(@"\b\d+\b", RegexOptions.Compiled);

    private static readonly HashSet<string> NumerosPermitidosEnAlimentos = new(StringComparer.Ordinal)
    {
        "0", "00", "000", "0000", "1", "2", "3", "4", "5", "7", "10", "20", "30", "35", "50", "60", "70", "80", "85", "90", "100"
    };

    private static readonly HashSet<string> PalabrasNoAlimento = new(StringComparer.Ordinal)
    {
        "auto", "automovil", "camion", "camioneta", "rastrojero", "moto", "motocicleta", "bicicleta", "patente",
        "modelo", "motor", "rueda", "neumatico", "llanta", "volante", "nafta", "combustible", "gasoil", "vehiculo",
        "celular", "telefono", "computadora", "notebook", "laptop", "televisor", "monitor", "teclado", "mouse",
        "auricular", "cargador", "cable", "enchufe", "pila", "bateria", "impresora", "consola", "joystick",
        "zapatilla", "zapato", "bota", "ojota", "remera", "camisa", "pantalon", "jean", "campera", "buzo",
        "vestido", "pollera", "media", "gorra", "sombrero", "mochila", "cartera", "billetera", "reloj",
        "silla", "mesa", "sillon", "cama", "colchon", "placard", "armario", "escritorio", "estante", "lampara",
        "martillo", "destornillador", "taladro", "tornillo", "clavo", "tuerca", "pinza", "llave", "herramienta",
        "cemento", "ladrillo", "pintura", "barniz", "madera", "hierro", "alambre", "vidrio", "plastico",
        "lavandina", "detergente", "jabon", "shampoo", "acondicionador", "desodorante", "perfume", "dentifrico",
        "cepillo", "esponja", "escoba", "trapo", "secador", "papel", "servilleta", "panal", "maquillaje",
        "ibuprofeno", "paracetamol", "aspirina", "antibiotico", "medicamento", "remedio", "pastilla", "jarabe",
        "pelota", "juguete", "muneca", "libro", "cuaderno", "lapicera", "lapiz", "regla", "carpeta",
        "alquiler", "impuesto", "factura", "sueldo", "dinero", "plata", "dolar", "peso", "tarjeta",
        "perro", "gato", "caballo", "casa", "departamento", "oficina", "colegio", "facultad"
    };

    private static readonly HashSet<string> AlimentosConocidos = new(StringComparer.Ordinal)
    {
        "agua", "sal", "pimienta", "aceite", "aceite de oliva", "aceite de girasol", "aceite de maiz",
        "aceite de coco", "vinagre", "aceto", "azucar", "azucar mascabo", "azucar impalpable", "azucar negra",
        "edulcorante", "stevia", "miel", "dulce de leche", "mermelada", "jalea",
        "harina", "harina 000", "harina 0000", "harina leudante", "harina integral", "harina de maiz",
        "harina de garbanzo", "harina de arroz", "harina de almendra", "harina de avena", "maicena", "almidon",
        "levadura", "polvo de hornear", "bicarbonato", "gelatina", "cacao", "chocolate", "vainilla",
        "esencia de vainilla", "coco", "coco rallado",
        "huevo", "clara", "yema", "leche", "leche entera", "leche descremada", "leche condensada",
        "leche en polvo", "leche de almendra", "leche de soja", "leche de avena", "leche de coco",
        "manteca", "mantequilla", "margarina", "crema", "crema de leche", "nata", "yogur", "yogurt",
        "queso", "queso crema", "queso cremoso", "queso rallado", "queso parmesano", "queso tybo",
        "queso cheddar", "queso roquefort", "queso azul", "queso fresco", "queso port salut",
        "muzzarella", "mozzarella", "ricota", "provolone", "sardo", "reggianito", "danbo",
        "pan", "pan lactal", "pan rallado", "rebozador", "tostada", "galleta", "galletita", "grisini",
        "tortilla", "tapa de empanada", "tapa de tarta", "masa", "hojaldre", "bizcochuelo",
        "arroz", "arroz integral", "arroz yamani", "fideos", "pasta", "tallarin", "spaghetti", "espagueti",
        "macarron", "tirabuzon", "mostachol", "ravioles", "noquis", "lasagna", "canelon", "polenta",
        "avena", "quinoa", "cuscus", "couscous", "trigo", "trigo burgol", "cebada", "centeno", "granola",
        "lentejas", "garbanzo", "poroto", "arveja", "habas", "alubias", "soja", "tofu", "seitan", "tempeh",
        "pollo", "pechuga", "pechuga de pollo", "muslo", "pata muslo", "suprema", "alita",
        "carne", "carne picada", "bife", "lomo", "nalga", "cuadril", "vacio", "asado", "matambre",
        "peceto", "paleta", "roast beef", "osobuco", "mondongo", "higado", "milanesa", "hamburguesa",
        "cerdo", "bondiola", "carre", "costilla", "pechito", "solomillo", "panceta", "tocino", "bacon",
        "jamon", "jamon cocido", "jamon crudo", "salame", "mortadela", "chorizo", "morcilla", "salchicha",
        "cordero", "pavo", "conejo",
        "pescado", "merluza", "salmon", "atun", "caballa", "sardina", "trucha", "abadejo", "lenguado",
        "bacalao", "anchoa", "camaron", "langostino", "calamar", "pulpo", "mejillon", "marisco",
        "ajo", "cebolla", "cebolla morada", "cebolla de verdeo", "verdeo", "puerro", "echalote", "chalota",
        "tomate", "tomate cherry", "tomate perita", "pure de tomate", "salsa de tomate", "extracto de tomate",
        "papa", "batata", "boniato", "mandioca", "zanahoria", "zapallo", "calabaza", "anco", "zapallito",
        "zucchini", "berenjena", "morron", "pimiento", "aji", "pepino", "lechuga", "rucula", "espinaca",
        "acelga", "radicheta", "repollo", "brocoli", "coliflor", "esparrago", "alcaucil", "apio", "hinojo",
        "remolacha", "rabanito", "nabo", "choclo", "maiz", "chaucha", "palmito", "champinon", "hongo",
        "portobello", "palta", "aguacate", "aceituna", "alcaparra", "pepino encurtido", "pickle",
        "manzana", "banana", "naranja", "mandarina", "limon", "lima", "pomelo", "pera", "durazno",
        "pelon", "ciruela", "damasco", "cereza", "frutilla", "frambuesa", "arandano", "mora", "uva",
        "kiwi", "anana", "pina", "mango", "papaya", "maracuya", "melon", "sandia", "higo", "membrillo",
        "nuez", "almendra", "avellana", "mani", "castana", "pistacho", "anacardo", "pecan", "pasa de uva",
        "datil", "ciruela pasa", "semilla", "sesamo", "chia", "lino", "girasol", "amapola",
        "oregano", "albahaca", "perejil", "cilantro", "romero", "tomillo", "laurel", "menta", "salvia",
        "eneldo", "estragon", "ciboulette", "comino", "pimenton", "paprika", "curcuma", "curry", "canela",
        "nuez moscada", "clavo de olor", "jengibre", "cardamomo", "anis", "azafran", "provenzal",
        "aji molido", "cayena", "pimienta negra", "pimienta blanca", "sal fina", "sal gruesa",
        "mostaza", "mayonesa", "ketchup", "salsa de soja", "salsa inglesa", "salsa golf", "salsa barbacoa",
        "chimichurri", "pesto", "hummus", "humus", "tahini", "caldo", "caldo de verdura", "caldo de pollo",
        "caldo de carne", "vino", "vino blanco", "vino tinto", "cerveza", "sidra", "jugo", "jugo de limon",
        "jugo de naranja", "cafe", "te", "yerba", "yerba mate", "mate cocido", "gaseosa", "soda"
    };

    private static readonly HashSet<string> RaicesAlimento = new(StringComparer.Ordinal)
    {
        "agua", "sal", "pimienta", "aceite", "vinagre", "azucar", "harina", "huevo", "leche", "manteca",
        "crema", "yogur", "yogurt", "queso", "pan", "arroz", "fideos", "pasta", "polenta", "avena", "quinoa",
        "lentejas", "garbanzo", "poroto", "arveja", "pollo", "carne", "cerdo", "pescado", "atun", "salmon",
        "merluza", "jamon", "panceta", "chorizo", "salchicha", "ajo", "cebolla", "tomate", "papa", "batata",
        "zanahoria", "zapallo", "calabaza", "zapallito", "berenjena", "morron", "pimiento", "lechuga",
        "espinaca", "acelga", "repollo", "brocoli", "coliflor", "choclo", "hongo", "champinon", "manzana",
        "banana", "naranja", "limon", "pera", "durazno", "frutilla", "uva", "palta", "nuez", "almendra",
        "mani", "semilla", "salsa", "caldo", "dulce", "mermelada", "miel", "chocolate", "cacao", "cafe",
        "te", "yerba", "jugo", "vino", "cerveza", "tarta", "empanada", "pizza", "galleta", "galletita",
        "masa", "tapa", "fideo", "pure", "extracto", "especia", "condimento", "hierba", "fruta", "verdura",
        "hortaliza", "legumbre", "cereal", "lacteo", "fiambre", "embutido", "marisco", "fruto"
    };

    private readonly IIngredienteNormalizerService _normalizer;
    private readonly IAsistenteIaClient _asistenteIa;
    private readonly IMemoryCache _cache;

    public IngredienteValidadorService(
        IIngredienteNormalizerService normalizer,
        IAsistenteIaClient asistenteIa,
        IMemoryCache cache)
    {
        _normalizer = normalizer;
        _asistenteIa = asistenteIa;
        _cache = cache;
    }

    public async Task AsegurarEsAlimentoAsync(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del ingrediente no puede estar vacío.");
        }

        var crudo = nombre.Trim();
        if (RegexCaracteresInvalidos.IsMatch(crudo))
        {
            throw new ArgumentException(MensajeInvalido);
        }

        var normalizado = _normalizer.Normalizar(crudo);
        if (normalizado.Length < MinLongitud || normalizado.Length > MaxLongitud)
        {
            throw new ArgumentException(MensajeInvalido);
        }

        if (normalizado.Count(char.IsLetter) < 2)
        {
            throw new ArgumentException(MensajeInvalido);
        }

        var palabras = normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (palabras.Length > MaxPalabras)
        {
            throw new ArgumentException(MensajeInvalido);
        }

        if (RegexAnio.IsMatch(normalizado) || TieneNumeroInvalido(normalizado))
        {
            throw new ArgumentException(MensajeInvalido);
        }

        if (palabras.Any(palabra => PalabrasNoAlimento.Contains(palabra)))
        {
            throw new ArgumentException(MensajeInvalido);
        }

        if (AlimentosConocidos.Contains(normalizado))
        {
            return;
        }

        var cacheKey = $"ingrediente:valido:{normalizado}";
        if (_cache.TryGetValue(cacheKey, out bool validoEnCache))
        {
            if (!validoEnCache)
            {
                throw new ArgumentException(MensajeInvalido);
            }

            return;
        }

        var esValido = await ValidarConIaAsync(normalizado, palabras);
        _cache.Set(cacheKey, esValido, CacheTtl);

        if (!esValido)
        {
            throw new ArgumentException(MensajeInvalido);
        }
    }

    private static bool TieneNumeroInvalido(string texto)
    {
        foreach (Match match in RegexNumeroSuelto.Matches(texto))
        {
            if (!NumerosPermitidosEnAlimentos.Contains(match.Value))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> ValidarConIaAsync(string normalizado, string[] palabras)
    {
        try
        {
            var json = await _asistenteIa.GenerarRespuestaJsonAsync(
                $"Texto a validar: \"{normalizado}\"",
                InstruccionValidacion);
            var limpio = JsonIa.LimpiarBloqueCodigo(json);
            var resultado = JsonSerializer.Deserialize<ValidacionAlimentoDto>(limpio, JsonOptions);
            if (resultado is not null)
            {
                return resultado.EsAlimento;
            }
        }
        catch (Exception ex) when (ex is ServicioExternoException or JsonException)
        {
        }

        return palabras.Length <= 4 && palabras.Any(palabra => RaicesAlimento.Contains(palabra));
    }

    private sealed class ValidacionAlimentoDto
    {
        [JsonPropertyName("esAlimento")]
        public bool EsAlimento { get; set; }
    }
}
