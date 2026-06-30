using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Application.DTOs.ChatIa;
using Kitch.Application.DTOs.Planificador;
using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ChatIaService : IChatIaService
{
    private const string InstruccionAgente =
        "Sos 'Kitch-AI', el asistente de cocina exclusivo de la plataforma Kitch. Estás hablando con __NOMBRE_USUARIO__. " +
        "Únicamente respondés temas de cocina: recetas, ingredientes, técnicas culinarias y planificación de comidas. " +
        "Si te preguntan algo ajeno a la cocina, rechazalo con amabilidad (accion 'conversar'). " +
        "Conocés la alacena del usuario (te la pasamos como contexto) y la usás para recomendar y avisar qué le falta.\n\n" +
        "RESPONDÉS SIEMPRE con un ÚNICO objeto JSON válido, sin texto extra ni markdown, con esta forma EXACTA:\n" +
        "{\"accion\": \"conversar\"|\"generar_receta\"|\"guardar_receta\"|\"sustituir\"|\"recomendar\"|\"eliminar_receta\"|\"planificar_receta\", " +
        "\"mensaje\": string, " +
        "\"receta\": {\"titulo\": string, \"descripcion\": string, \"tiempoPreparacionMinutos\": number, " +
        "\"porciones\": number, \"dificultad\": \"Facil\"|\"Medio\"|\"Dificil\", \"caloriasEstimadas\": number, " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}], \"pasos\": [string]} | null, " +
        "\"ingredienteSustituir\": string | null, \"tituloEliminar\": string | null, \"eliminarTodas\": boolean, " +
        "\"tituloPlanificar\": string | null, \"fechaPlanificar\": string | null, \"turnoPlanificar\": string | null}\n\n" +
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
        "- 'eliminar_receta': el usuario quiere borrar/eliminar recetas guardadas. " +
        "Si menciona UNA receta puntual (ej. 'borrá la de tortilla', 'eliminá la tarta de manzana'), poné solo el nombre " +
        "(sin comillas, tal como lo dijo) en 'tituloEliminar' y dejá 'eliminarTodas' en false. " +
        "Si quiere borrar TODAS (ej. 'borrá todas mis recetas', 'eliminá todos mis favoritos', 'borrá todo', 'limpiá mis favoritos'), " +
        "poné 'eliminarTodas' en true y 'tituloEliminar' en null. En 'mensaje' confirmás. " +
        "El sistema borra solo recetas que el usuario tenga guardadas.\n" +
        "- 'planificar_receta': el usuario quiere agendar/planificar una receta para una fecha y momento del día " +
        "(ej. 'planificá la tortilla para mañana al mediodía', 'agendá esta receta para el viernes a la cena'). " +
        "En 'tituloPlanificar' poné el nombre de la receta (o dejalo null si se refiere a la última receta de la conversación). " +
        "En 'fechaPlanificar' poné la fecha en formato YYYY-MM-DD (resolvé 'hoy', 'mañana', 'el viernes' usando la 'Fecha de hoy' del contexto). " +
        "En 'turnoPlanificar' poné el momento: 'Desayuno', 'Almuerzo', 'Merienda' o 'Cena'. " +
        "En 'mensaje' confirmás. El sistema agrega solo lo que el usuario tenga guardado y, si le faltan ingredientes, " +
        "los suma automáticamente a su lista de compras.\n" +
        "Nunca inventes que guardaste, borraste o planificaste algo si la acción no fue la correspondiente.";

    private const string NombrePorDefecto = "Chef";

    private static readonly ConcurrentDictionary<int, RecetaGeneradaDto> UltimaRecetaPorUsuario = new();

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
    private readonly IPlanificadorService _planificadorService;

    public ChatIaService(
        IGeminiClient geminiClient,
        IRepository<Usuario> usuarioRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRecetaIaService recetaIaService,
        ISustitucionService sustitucionService,
        IRecomendacionService recomendacionService,
        IRepository<Receta> recetaRepository,
        IRepository<RecetaFavorita> favoritoRepository,
        IPlanificadorService planificadorService)
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
        _planificadorService = planificadorService;
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

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        var nombreUsuario = string.IsNullOrWhiteSpace(usuario?.Nombre) ? NombrePorDefecto : usuario!.Nombre;

        var systemInstruction = InstruccionAgente.Replace("__NOMBRE_USUARIO__", nombreUsuario);

        var contexto = await ConstruirContextoAsync(usuarioId, request.RecetaActual);
        var mensajes = ConstruirConversacion(contexto, request);

        string json;
        try
        {
            json = await _geminiClient.GenerarRespuestaConversacionAsync(mensajes, systemInstruction, jsonMode: true);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "El asistente está recibiendo demasiadas consultas en este momento " +
                    "(se alcanzó el límite de uso de la API de IA). Esperá unos segundos y volvé a intentar."
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "El asistente de IA está sobrecargado en este momento (problema temporal del proveedor). " +
                    "Esperá unos segundos y volvé a intentar."
            };
        }
        catch (HttpRequestException)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No pude conectarme con el asistente de IA en este momento. Probá de nuevo en unos instantes."
            };
        }

        var sobre = DeserializarSobre(json);

        if (sobre is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "Perdón, no pude procesar la respuesta. ¿Podés reformular tu pedido?"
            };
        }

        return (sobre.Accion ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            ChatAccion.GenerarReceta => await ResolverGenerarRecetaAsync(usuarioId, sobre),
            ChatAccion.GuardarReceta => await ResolverGuardarRecetaAsync(usuarioId, sobre, request.RecetaActual),
            ChatAccion.Sustituir => await ResolverSustituirAsync(usuarioId, sobre),
            ChatAccion.Recomendar => await ResolverRecomendarAsync(usuarioId, sobre),
            ChatAccion.EliminarReceta => await ResolverEliminarRecetaAsync(usuarioId, sobre),
            ChatAccion.PlanificarReceta => await ResolverPlanificarRecetaAsync(usuarioId, sobre),
            _ => new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                    ? "Contame qué querés cocinar y te ayudo."
                    : sobre.Mensaje
            }
        };
    }

    private async Task<ChatRespuestaDto> ResolverGenerarRecetaAsync(int usuarioId, SobreAgente sobre)
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

        receta.Titulo = RecetaIaService.GenerarTituloPorDefecto(receta.Titulo, receta.Ingredientes);

        UltimaRecetaPorUsuario[usuarioId] = receta;

        try
        {
            await _recetaIaService.AsegurarIngredientesEnCatalogoAsync(receta);
        }
        catch
        {
        }

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
        var receta = ElegirRecetaParaGuardar(usuarioId, sobre.Receta, recetaActual);

        if (receta is null)
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

            UltimaRecetaPorUsuario.TryRemove(usuarioId, out _);
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

    private RecetaGeneradaDto? ElegirRecetaParaGuardar(
        int usuarioId,
        RecetaGeneradaDto? delSobre,
        RecetaGeneradaDto? recetaActual)
    {
        if (EsRecetaValida(delSobre))
        {
            return delSobre;
        }

        if (EsRecetaValida(recetaActual))
        {
            return recetaActual;
        }

        return UltimaRecetaPorUsuario.TryGetValue(usuarioId, out var recordada) ? recordada : null;
    }

    private static bool EsRecetaValida(RecetaGeneradaDto? receta)
    {
        if (receta is null || receta.Ingredientes.Count == 0)
        {
            return false;
        }

        var titulo = receta.Titulo?.Trim();
        return !string.IsNullOrWhiteSpace(titulo) &&
            !titulo.Equals("string", StringComparison.OrdinalIgnoreCase);
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

        var ingrediente = await _ingredienteRepository.FirstOrDefaultAsync(i => i.Nombre == nombre);
        ingrediente ??= await _ingredienteRepository.AddAsync(new Ingrediente
        {
            Nombre = nombre,
            Categoria = "Varios"
        });

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

        if (!sobre.EliminarTodas && string.IsNullOrWhiteSpace(titulo))
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "¿Querés borrar una receta puntual (decime el nombre) o todas tus recetas guardadas?"
            };
        }

        var favoritos = await _favoritoRepository.FindWithIncludesAsync(
            favorito => favorito.UsuarioId == usuarioId,
            favorito => favorito.Receta);

        var recetas = favoritos
            .Select(favorito => favorito.Receta)
            .Where(receta => receta is not null)
            .GroupBy(receta => receta!.Id)
            .Select(grupo => grupo.First()!)
            .ToList();

        if (!sobre.EliminarTodas)
        {
            recetas = recetas
                .Where(receta => CoincideTitulo(receta.Titulo, titulo!))
                .ToList();
        }

        if (recetas.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.EliminarReceta,
                Mensaje = sobre.EliminarTodas
                    ? "No tenés recetas guardadas para borrar."
                    : $"No encontré ninguna receta guardada que coincida con \"{titulo}\".",
                RecetasEliminadas = 0
            };
        }

        foreach (var receta in recetas)
        {
            await _recetaRepository.DeleteAsync(receta);
        }

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.EliminarReceta,
            Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                ? (sobre.EliminarTodas
                    ? $"Listo, borré tus {recetas.Count} receta(s) guardada(s)."
                    : $"Listo, borré {recetas.Count} receta(s) que coincidían con \"{titulo}\".")
                : sobre.Mensaje,
            RecetasEliminadas = recetas.Count
        };
    }

    private async Task<ChatRespuestaDto> ResolverPlanificarRecetaAsync(int usuarioId, SobreAgente sobre)
    {
        var (recetaId, tituloReceta, errorReceta) = await ResolverRecetaParaPlanificarAsync(usuarioId, sobre);
        if (recetaId is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = errorReceta!
            };
        }

        var fecha = ParsearFecha(sobre.FechaPlanificar);

        var turno = string.IsNullOrWhiteSpace(sobre.TurnoPlanificar) ? "Almuerzo" : sobre.TurnoPlanificar!.Trim();

        try
        {
            var resultado = await _planificadorService.PlanificarAsync(new ComidaPlanificadaCreateDto
            {
                UsuarioId = usuarioId,
                RecetaId = recetaId.Value,
                FechaAsignada = fecha,
                Turno = turno
            });

            var agregados = resultado.IngredientesAgregadosALista;
            var aviso = agregados.Count > 0
                ? $" Te faltaban algunos ingredientes, así que los agregué a tu lista de compras: {string.Join(", ", agregados)}."
                : " Ya tenías todos los ingredientes en tu alacena.";

            return new ChatRespuestaDto
            {
                Accion = ChatAccion.PlanificarReceta,
                Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                    ? $"Planifiqué \"{tituloReceta}\" para el {fecha:dd/MM/yyyy} ({turno})." + aviso
                    : sobre.Mensaje + aviso,
                ComidaPlanificada = resultado.Comida,
                IngredientesAgregadosALista = agregados
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = ex.Message
            };
        }
    }

    private async Task<(int? recetaId, string? titulo, string? error)> ResolverRecetaParaPlanificarAsync(
        int usuarioId,
        SobreAgente sobre)
    {
        var titulo = sobre.TituloPlanificar?.Trim();

        if (!string.IsNullOrWhiteSpace(titulo))
        {
            var favoritos = await _favoritoRepository.FindWithIncludesAsync(
                favorito => favorito.UsuarioId == usuarioId,
                favorito => favorito.Receta);

            var receta = favoritos
                .Select(favorito => favorito.Receta)
                .FirstOrDefault(r => r is not null && CoincideTitulo(r.Titulo, titulo));

            if (receta is not null)
            {
                return (receta.Id, receta.Titulo, null);
            }
        }

        if (UltimaRecetaPorUsuario.TryGetValue(usuarioId, out var recordada) && EsRecetaValida(recordada))
        {
            try
            {
                var guardada = await _recetaIaService.GuardarRecetaAsync(usuarioId, recordada);
                return (guardada.RecetaId, guardada.Titulo, null);
            }
            catch (InvalidOperationException ex)
            {
                return (null, null, $"No pude preparar la receta para planificarla: {ex.Message}");
            }
        }

        return (null, null, "No sé qué receta planificar. Generá o guardá una receta primero, " +
            "o decime el nombre de una que ya tengas guardada.");
    }

    private static DateTime ParsearFecha(string? fecha)
    {
        if (!string.IsNullOrWhiteSpace(fecha) &&
            DateTime.TryParse(fecha, out var parseada))
        {
            return parseada.Date;
        }

        return DateTime.Today;
    }

    private static bool CoincideTitulo(string tituloReceta, string busqueda)
    {
        var a = tituloReceta.Trim();
        var b = busqueda.Trim();

        return a.Equals(b, StringComparison.OrdinalIgnoreCase) ||
            a.Contains(b, StringComparison.OrdinalIgnoreCase) ||
            b.Contains(a, StringComparison.OrdinalIgnoreCase);
    }

    private List<MensajeIa> ConstruirConversacion(string contexto, ChatRequestDto request)
    {
        var mensajes = new List<MensajeIa>();

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
        contexto.AppendLine($"Fecha de hoy: {DateTime.Today:yyyy-MM-dd} ({DateTime.Today:dddd}).");
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

        var recetaContexto = EsRecetaValida(recetaActual)
            ? recetaActual
            : (UltimaRecetaPorUsuario.TryGetValue(usuarioId, out var recordada) ? recordada : null);

        if (recetaContexto is not null)
        {
            contexto.AppendLine();
            contexto.AppendLine("Última receta de la conversación (úsala si pide guardarla, modificarla o referirse a 'la receta'):");
            contexto.AppendLine(JsonSerializer.Serialize(recetaContexto, JsonOptions));
        }

        return contexto.ToString();
    }

    private static SobreAgente? DeserializarSobre(string json)
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
            return JsonSerializer.Deserialize<SobreAgente>(limpio, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

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

        [JsonPropertyName("eliminarTodas")]
        public bool EliminarTodas { get; set; }

        [JsonPropertyName("tituloPlanificar")]
        public string? TituloPlanificar { get; set; }

        [JsonPropertyName("fechaPlanificar")]
        public string? FechaPlanificar { get; set; }

        [JsonPropertyName("turnoPlanificar")]
        public string? TurnoPlanificar { get; set; }
    }
}
