using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Application.DTOs.ChatIa;
using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ChatIaService : IChatIaService
{
    // System prompt maestro: define al agente y, sobre todo, lo obliga a responder SIEMPRE
    // con un único JSON cuyo campo "accion" le dice al servidor qué efecto ejecutar.
    // De esta forma chatear, generar, guardar, sustituir y recomendar viven en un solo flujo.
    private const string InstruccionAgente =
        "Sos 'Kitch-AI', el asistente de cocina exclusivo de la plataforma Kitch. Estás hablando con __NOMBRE_USUARIO__. " +
        "Únicamente respondés temas de cocina: recetas, ingredientes, técnicas culinarias y planificación de comidas. " +
        "Si te preguntan algo ajeno a la cocina, rechazalo con amabilidad (accion 'conversar'). " +
        "Conocés la alacena del usuario (te la pasamos como contexto) y la usás para recomendar y avisar qué le falta.\n\n" +
        "RESPONDÉS SIEMPRE con un ÚNICO objeto JSON válido, sin texto extra ni markdown, con esta forma EXACTA:\n" +
        "{\"accion\": \"conversar\"|\"generar_receta\"|\"guardar_receta\"|\"sustituir\"|\"recomendar\"|\"eliminar_receta\", " +
        "\"mensaje\": string, " +
        "\"receta\": {\"titulo\": string, \"descripcion\": string, \"tiempoPreparacionMinutos\": number, " +
        "\"porciones\": number, \"dificultad\": \"Facil\"|\"Medio\"|\"Dificil\", \"caloriasEstimadas\": number, " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}], \"pasos\": [string]} | null, " +
        "\"ingredienteSustituir\": string | null, \"tituloEliminar\": string | null}\n\n" +
        "REGLAS DE LA 'accion':\n" +
        "- 'conversar': charla general, dudas, consejos. 'receta' va en null.\n" +
        "- 'generar_receta': el usuario pide una receta. Completás 'receta' (mín. 1 ingrediente y 1 paso, " +
        "tiempo y porciones > 0). Usá preferentemente lo que hay en la alacena. En 'mensaje' presentás la receta. " +
        "SIEMPRE poné en 'titulo' un nombre descriptivo y real del plato (ej. 'Tortilla de papas'); NUNCA uses 'string', " +
        "'receta' ni dejes el título vacío. Si el usuario no aclara el nombre, inventá uno acorde a los ingredientes.\n" +
        "- 'guardar_receta': el usuario quiere conservar/guardar la última receta o agregarla a favoritos. " +
        "Interpretá la INTENCIÓN, no palabras exactas: valen frases como 'guardala', 'guarda la receta', " +
        "'guardámela', 'agregala a favoritos', 'dale, sumala', 'me encantó, la quiero tener', 'sí, está buena, guardala'. " +
        "VOLVÉS a incluir la receta completa en 'receta' (tomala del contexto de la conversación o de la 'Receta actual'). " +
        "En 'mensaje' confirmás que la guardaste.\n" +
        "- 'sustituir': el usuario pregunta con qué reemplazar un ingrediente. Poné el nombre del ingrediente " +
        "original en 'ingredienteSustituir'. En 'mensaje' explicás brevemente; el sistema completa la lista de sustitutos.\n" +
        "- 'recomendar': el usuario pide ideas/recetas según lo que tiene. 'receta' va en null; el sistema agrega " +
        "las recetas compatibles. En 'mensaje' introducís las recomendaciones.\n" +
        "- 'eliminar_receta': el usuario quiere borrar/eliminar una receta guardada (ej. 'borrá la receta de tortilla', " +
        "'eliminá la tarta de manzana'). Poné el nombre de la receta a borrar en 'tituloEliminar'. En 'mensaje' confirmás. " +
        "El sistema borra solo recetas que el usuario tenga guardadas.\n" +
        "Nunca inventes que guardaste o borraste algo si la acción no fue 'guardar_receta' o 'eliminar_receta'.";

    private const string NombrePorDefecto = "Chef";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IGeminiClient _geminiClient;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRecetaIaService _recetaIaService;
    private readonly ISustitucionService _sustitucionService;
    private readonly IRecomendacionService _recomendacionService;
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<RecetaFavorita> _favoritoRepository;

    public ChatIaService(
        IGeminiClient geminiClient,
        IRepository<Usuario> usuarioRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRecetaIaService recetaIaService,
        ISustitucionService sustitucionService,
        IRecomendacionService recomendacionService,
        IRepository<Receta> recetaRepository,
        IRepository<RecetaFavorita> favoritoRepository)
    {
        _geminiClient = geminiClient;
        _usuarioRepository = usuarioRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
        _recetaIaService = recetaIaService;
        _sustitucionService = sustitucionService;
        _recomendacionService = recomendacionService;
        _recetaRepository = recetaRepository;
        _favoritoRepository = favoritoRepository;
    }

    public async Task<ChatRespuestaDto> ProcesarMensajeAsync(int usuarioId, ChatRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Mensaje))
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "Escribime algo para poder ayudarte con tu cocina."
            };
        }

        // 1. Armamos el system prompt con el nombre del usuario.
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        var nombreUsuario = string.IsNullOrWhiteSpace(usuario?.Nombre) ? NombrePorDefecto : usuario!.Nombre;

        // Usamos siempre el prompt del agente: es el único que define las acciones (generar,
        // guardar, sustituir, recomendar). El de appsettings era solo para chatear y lo dejamos de lado.
        var systemInstruction = InstruccionAgente.Replace("__NOMBRE_USUARIO__", nombreUsuario);

        // 2. Construimos la conversación: contexto + historial + mensaje nuevo.
        var contexto = await ConstruirContextoAsync(usuarioId, request.RecetaActual);
        var mensajes = ConstruirConversacion(contexto, request);

        // 3. Le pedimos al modelo el sobre JSON con la acción a ejecutar.
        var json = await _geminiClient.GenerarRespuestaConversacionAsync(mensajes, systemInstruction, jsonMode: true);
        var sobre = DeserializarSobre(json);

        if (sobre is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "Perdón, no pude procesar la respuesta. ¿Podés reformular tu pedido?"
            };
        }

        // 4. Ejecutamos el efecto según la acción que decidió el agente.
        return (sobre.Accion ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            ChatAccion.GenerarReceta => await ResolverGenerarRecetaAsync(sobre),
            ChatAccion.GuardarReceta => await ResolverGuardarRecetaAsync(usuarioId, sobre, request.RecetaActual),
            ChatAccion.Sustituir => await ResolverSustituirAsync(usuarioId, sobre),
            ChatAccion.Recomendar => await ResolverRecomendarAsync(usuarioId, sobre),
            ChatAccion.EliminarReceta => await ResolverEliminarRecetaAsync(usuarioId, sobre),
            _ => new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                    ? "Contame qué querés cocinar y te ayudo."
                    : sobre.Mensaje
            }
        };
    }

    private async Task<ChatRespuestaDto> ResolverGenerarRecetaAsync(SobreAgente sobre)
    {
        var receta = sobre.Receta;
        if (receta is null || receta.Ingredientes.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No pude armar una receta válida esta vez. Probá pedírmela de nuevo o cargá más ingredientes en tu alacena."
            };
        }

        // Si el agente no puso un título válido, generamos uno descriptivo (nunca "string").
        receta.Titulo = RecetaIaService.GenerarTituloPorDefecto(receta.Titulo, receta.Ingredientes);

        // Damos de alta automáticamente en el catálogo los ingredientes usados.
        await _recetaIaService.AsegurarIngredientesEnCatalogoAsync(receta);

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.GenerarReceta,
            Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                ? $"Te propongo: {receta.Titulo}. Si querés, decime \"guardala\" y la agrego a tus favoritos."
                : sobre.Mensaje,
            Receta = receta
        };
    }

    private async Task<ChatRespuestaDto> ResolverGuardarRecetaAsync(
        int usuarioId,
        SobreAgente sobre,
        RecetaGeneradaDto? recetaActual)
    {
        // La receta puede venir en el sobre (la IA la repite) o, como respaldo,
        // en lo que el usuario tenía en pantalla.
        var receta = sobre.Receta ?? recetaActual;

        if (receta is null || string.IsNullOrWhiteSpace(receta.Titulo) || receta.Ingredientes.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No encuentro una receta para guardar. Primero pedime que te genere una y después decime que la guarde."
            };
        }

        try
        {
            var guardada = await _recetaIaService.GuardarRecetaAsync(usuarioId, receta);
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.GuardarReceta,
                Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                    ? guardada.Mensaje
                    : sobre.Mensaje,
                RecetaGuardada = guardada
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = $"No pude guardar la receta: {ex.Message}"
            };
        }
    }

    private async Task<ChatRespuestaDto> ResolverSustituirAsync(int usuarioId, SobreAgente sobre)
    {
        var nombre = sobre.IngredienteSustituir?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "¿Qué ingrediente querés reemplazar?"
            };
        }

        // El ingrediente original tiene que existir en el catálogo para poder asociarle sustitutos.
        var ingrediente = await _ingredienteRepository.FirstOrDefaultAsync(i => i.Nombre == nombre);
        ingrediente ??= await _ingredienteRepository.AddAsync(new Ingrediente
        {
            Nombre = nombre,
            Categoria = "Varios"
        });

        // El servicio de sustitución genera (vía IA) y persiste los sustitutos si no existían,
        // y los devuelve priorizando los que el usuario ya tiene en la alacena.
        var sustitutos = (await _sustitucionService.BuscarSustitutosAsync(usuarioId, ingrediente.Id)).ToList();

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.Sustituir,
            Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                ? $"Estos son los reemplazos que te recomiendo para {nombre}:"
                : sobre.Mensaje,
            IngredienteSustituido = ingrediente.Nombre,
            Sustitutos = sustitutos
        };
    }

    private async Task<ChatRespuestaDto> ResolverRecomendarAsync(int usuarioId, SobreAgente sobre)
    {
        var recomendaciones = (await _recomendacionService.RecomendarAsync(usuarioId)).ToList();

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.Recomendar,
            Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                ? (recomendaciones.Count == 0
                    ? "Todavía no tengo recetas para recomendarte. Cargá ingredientes en tu alacena o generá una receta nueva."
                    : "Según lo que tenés en tu alacena, estas son las recetas que más te convienen:")
                : sobre.Mensaje,
            Recomendaciones = recomendaciones
        };
    }

    private async Task<ChatRespuestaDto> ResolverEliminarRecetaAsync(int usuarioId, SobreAgente sobre)
    {
        var titulo = sobre.TituloEliminar?.Trim();
        if (string.IsNullOrWhiteSpace(titulo))
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "¿Cuál receta querés que borre? Decime el título."
            };
        }

        // Solo borramos recetas que el usuario tenga guardadas en favoritos: así nadie
        // puede eliminar recetas de otros usuarios desde el chat.
        var favoritos = await _favoritoRepository.FindWithIncludesAsync(
            favorito => favorito.UsuarioId == usuarioId,
            favorito => favorito.Receta);

        var recetas = favoritos
            .Select(favorito => favorito.Receta)
            .Where(receta => receta is not null &&
                string.Equals(receta.Titulo.Trim(), titulo, StringComparison.OrdinalIgnoreCase))
            .GroupBy(receta => receta!.Id)
            .Select(grupo => grupo.First()!)
            .ToList();

        if (recetas.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.EliminarReceta,
                Mensaje = $"No encontré ninguna receta guardada con el título \"{titulo}\".",
                RecetasEliminadas = 0
            };
        }

        // Borrar la receta arrastra en cascada sus ingredientes, pasos, favoritos y planificación.
        foreach (var receta in recetas)
        {
            await _recetaRepository.DeleteAsync(receta);
        }

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.EliminarReceta,
            Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                ? $"Listo, borré \"{titulo}\" de tus recetas."
                : sobre.Mensaje,
            RecetasEliminadas = recetas.Count
        };
    }

    private List<MensajeIa> ConstruirConversacion(string contexto, ChatRequestDto request)
    {
        var mensajes = new List<MensajeIa>();

        // Primer turno "user" con el contexto de la alacena/receta actual.
        mensajes.Add(new MensajeIa("user", contexto));

        if (request.Historial is not null)
        {
            foreach (var turno in request.Historial)
            {
                if (string.IsNullOrWhiteSpace(turno.Texto))
                {
                    continue;
                }

                var rol = string.Equals(turno.Rol, "asistente", StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user";
                mensajes.Add(new MensajeIa(rol, turno.Texto));
            }
        }

        mensajes.Add(new MensajeIa("user", request.Mensaje));
        return mensajes;
    }

    private async Task<string> ConstruirContextoAsync(int usuarioId, RecetaGeneradaDto? recetaActual)
    {
        var stock = await _stockRepository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId && item.Cantidad > 0,
            item => item.Ingrediente);

        var contexto = new StringBuilder();
        contexto.AppendLine("[CONTEXTO PARA EL ASISTENTE]");
        contexto.AppendLine("Ingredientes que el usuario tiene actualmente en su alacena:");

        if (stock.Count == 0)
        {
            contexto.AppendLine("- (la alacena está vacía)");
        }
        else
        {
            foreach (var item in stock)
            {
                var nombre = string.IsNullOrWhiteSpace(item.Ingrediente?.Nombre)
                    ? $"Ingrediente #{item.IngredienteId}"
                    : item.Ingrediente!.Nombre;
                contexto.AppendLine($"- {nombre}: {item.Cantidad} {item.UnidadMedida}");
            }
        }

        if (recetaActual is not null && !string.IsNullOrWhiteSpace(recetaActual.Titulo))
        {
            contexto.AppendLine();
            contexto.AppendLine("Receta actual que el usuario tiene en pantalla (úsala si pide guardarla):");
            contexto.AppendLine(JsonSerializer.Serialize(recetaActual, JsonOptions));
        }

        return contexto.ToString();
    }

    private static SobreAgente? DeserializarSobre(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        // Por las dudas que el modelo envuelva el JSON en ```json ... ```, lo limpiamos.
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
            return JsonSerializer.Deserialize<SobreAgente>(limpio, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Estructura del JSON que devuelve el agente. El servidor la interpreta y ejecuta los efectos.
    private sealed class SobreAgente
    {
        [JsonPropertyName("accion")]
        public string? Accion { get; set; }

        [JsonPropertyName("mensaje")]
        public string? Mensaje { get; set; }

        [JsonPropertyName("receta")]
        public RecetaGeneradaDto? Receta { get; set; }

        [JsonPropertyName("ingredienteSustituir")]
        public string? IngredienteSustituir { get; set; }

        [JsonPropertyName("tituloEliminar")]
        public string? TituloEliminar { get; set; }
    }
}
