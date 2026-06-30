using System.Text;
using System.Text.Json;
using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class RecetaIaService : IRecetaIaService
{
    private const string InstruccionGeneracion =
        "Sos 'Kitch-AI', un chef experto de la plataforma Kitch. Tu tarea es crear UNA receta " +
        "usando preferentemente los ingredientes que el usuario tiene en su alacena. " +
        "Podés sumar ingredientes básicos comunes (sal, agua, aceite) si hacen falta. " +
        "Respondé ÚNICAMENTE con un objeto JSON válido, sin texto adicional ni markdown, con esta forma exacta: " +
        "{\"titulo\": string, \"descripcion\": string, \"tiempoPreparacionMinutos\": number, " +
        "\"porciones\": number, \"dificultad\": \"Facil\"|\"Medio\"|\"Dificil\", \"caloriasEstimadas\": number, " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}], " +
        "\"pasos\": [string]}. " +
        "El tiempo y las porciones deben ser mayores a cero, y tiene que haber al menos un ingrediente y un paso.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAsistenteIaClient _asistenteIa;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<RecetaFavorita> _favoritoRepository;

    public RecetaIaService(
        IAsistenteIaClient asistenteIa,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRepository<Receta> recetaRepository,
        IRepository<RecetaFavorita> favoritoRepository)
    {
        _asistenteIa = asistenteIa;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
        _recetaRepository = recetaRepository;
        _favoritoRepository = favoritoRepository;
    }

    public async Task<RecetaGeneradaDto> GenerarRecetaAsync(int usuarioId, string? preferencias)
    {
        var stock = await _stockRepository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId && item.Cantidad > 0,
            item => item.Ingrediente);

        var prompt = new StringBuilder();
        prompt.AppendLine("Ingredientes disponibles en la alacena del usuario:");

        if (stock.Count == 0)
        {
            prompt.AppendLine("- (la alacena está vacía: proponé una receta sencilla con ingredientes básicos)");
        }
        else
        {
            foreach (var item in stock)
            {
                var nombre = string.IsNullOrWhiteSpace(item.Ingrediente?.Nombre)
                    ? $"Ingrediente #{item.IngredienteId}"
                    : item.Ingrediente!.Nombre;
                prompt.AppendLine($"- {nombre}: {item.Cantidad} {item.UnidadMedida}");
            }
        }

        if (!string.IsNullOrWhiteSpace(preferencias))
        {
            prompt.AppendLine();
            prompt.AppendLine($"Preferencias o restricciones del usuario: {preferencias}");
        }

        var json = await _asistenteIa.GenerarRespuestaJsonAsync(prompt.ToString(), InstruccionGeneracion);

        var receta = DeserializarReceta(json);

        if (receta is null || string.IsNullOrWhiteSpace(receta.Titulo) || receta.Ingredientes.Count == 0)
        {
            throw new InvalidOperationException(
                "La IA no devolvió una receta válida. Intentá nuevamente o cargá más ingredientes en tu alacena.");
        }

        return receta;
    }

    public async Task<RecetaGuardadaResponse> GuardarRecetaAsync(int usuarioId, RecetaGeneradaDto receta)
    {
        if (receta is null)
        {
            throw new InvalidOperationException("No se recibió la receta a guardar.");
        }

        var titulo = GenerarTituloPorDefecto(receta.Titulo, receta.Ingredientes);

        var ingredientesValidos = receta.Ingredientes
            .Where(ingrediente => !string.IsNullOrWhiteSpace(ingrediente.Nombre))
            .ToList();
        var pasosValidos = receta.Pasos
            .Where(paso => !string.IsNullOrWhiteSpace(paso))
            .ToList();

        if (ingredientesValidos.Count == 0)
        {
            throw new InvalidOperationException("La receta debe tener al menos un ingrediente.");
        }

        if (pasosValidos.Count == 0)
        {
            throw new InvalidOperationException("La receta debe tener al menos un paso de preparación.");
        }

        var ingredientesReceta = new List<IngredienteReceta>();
        var nombresProcesados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ingrediente in ingredientesValidos)
        {
            var nombre = ingrediente.Nombre.Trim();

            if (!nombresProcesados.Add(nombre))
            {
                continue;
            }

            var ingredienteId = await ObtenerOCrearIngredienteAsync(nombre);

            ingredientesReceta.Add(new IngredienteReceta
            {
                IngredienteId = ingredienteId,
                Cantidad = ingrediente.Cantidad,
                UnidadMedida = string.IsNullOrWhiteSpace(ingrediente.UnidadMedida)
                    ? "u"
                    : ingrediente.UnidadMedida.Trim()
            });
        }

        var pasos = pasosValidos
            .Select((paso, indice) => new PreparacionReceta
            {
                NumeroPaso = indice + 1,
                DescripcionPaso = paso.Trim()
            })
            .ToList();

        var nuevaReceta = new Receta
        {
            Titulo = titulo,
            Descripcion = receta.Descripcion?.Trim() ?? string.Empty,
            TiempoPreparacionMinutos = receta.TiempoPreparacionMinutos > 0 ? receta.TiempoPreparacionMinutos : 1,
            Porciones = receta.Porciones > 0 ? receta.Porciones : 1,
            CaloriasEstimadas = receta.CaloriasEstimadas < 0 ? 0 : receta.CaloriasEstimadas,
            Dificultad = ParsearDificultad(receta.Dificultad),
            IngredientesReceta = ingredientesReceta,
            Preparaciones = pasos
        };

        var recetaCreada = await _recetaRepository.AddAsync(nuevaReceta);

        await _favoritoRepository.AddAsync(new RecetaFavorita
        {
            UsuarioId = usuarioId,
            RecetaId = recetaCreada.Id
        });

        return new RecetaGuardadaResponse
        {
            RecetaId = recetaCreada.Id,
            Titulo = recetaCreada.Titulo,
            Favorita = true,
            Mensaje = "Receta guardada y agregada a tus favoritos."
        };
    }

    public async Task AsegurarIngredientesEnCatalogoAsync(RecetaGeneradaDto receta)
    {
        if (receta is null)
        {
            return;
        }

        var nombresProcesados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ingrediente in receta.Ingredientes)
        {
            var nombre = ingrediente.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre) || !nombresProcesados.Add(nombre))
            {
                continue;
            }

            await ObtenerOCrearIngredienteAsync(nombre);
        }
    }

    private async Task<int> ObtenerOCrearIngredienteAsync(string nombre)
    {
        var existente = await _ingredienteRepository.FirstOrDefaultAsync(
            ingrediente => ingrediente.Nombre == nombre);

        if (existente is not null)
        {
            return existente.Id;
        }

        var creado = await _ingredienteRepository.AddAsync(new Ingrediente
        {
            Nombre = nombre,
            Categoria = "Varios"
        });

        return creado.Id;
    }

    public static string GenerarTituloPorDefecto(string? titulo, IEnumerable<IngredienteGeneradoDto> ingredientes)
    {
        var limpio = titulo?.Trim();
        if (!string.IsNullOrWhiteSpace(limpio) &&
            !limpio.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return limpio;
        }

        var nombres = (ingredientes ?? [])
            .Select(ingrediente => ingrediente.Nombre?.Trim())
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Take(2)
            .ToList();

        return nombres.Count > 0
            ? $"Receta con {string.Join(" y ", nombres)}"
            : "Receta sin título";
    }

    private static DificultadReceta ParsearDificultad(string? dificultad)
    {
        return Enum.TryParse<DificultadReceta>(dificultad?.Trim(), ignoreCase: true, out var resultado)
            ? resultado
            : DificultadReceta.Medio;
    }

    private static RecetaGeneradaDto? DeserializarReceta(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
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

        try
        {
            return JsonSerializer.Deserialize<RecetaGeneradaDto>(limpio, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
