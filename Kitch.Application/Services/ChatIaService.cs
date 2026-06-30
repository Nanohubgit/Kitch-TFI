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
    // System prompt maestro: define al agente y, sobre todo, lo obliga a responder SIEMPRE
    // con un único JSON cuyo campo "accion" le dice al servidor qué efecto ejecutar.
    // De esta forma chatear, generar, guardar, sustituir y recomendar viven en un solo flujo.
    private const string InstruccionAgente =
        "Sos 'Kitch-AI', el asistente de cocina exclusivo de la plataforma Kitch. Estás hablando con __NOMBRE_USUARIO__. " +
        "Únicamente respondés temas de cocina: recetas, ingredientes, técnicas culinarias y planificación de comidas. " +
        "Si te preguntan algo ajeno a la cocina, rechazalo con amabilidad (accion 'conversar'). " +
        "Conocés la alacena del usuario (te la pasamos como contexto) y la usás para recomendar y avisar qué le falta.\n\n" +
        "RESPONDÉS SIEMPRE con un ÚNICO objeto JSON válido, sin texto extra ni markdown, con esta forma EXACTA:\n" +
        "{\"accion\": \"conversar\"|\"generar_receta\"|\"guardar_receta\"|\"sustituir\"|\"recomendar\"|\"eliminar_receta\"|\"planificar_receta\"|\"cocinar_receta\", " +
        "\"mensaje\": string, " +
        "\"receta\": {\"titulo\": string, \"descripcion\": string, \"tiempoPreparacionMinutos\": number, " +
        "\"porciones\": number, \"dificultad\": \"Facil\"|\"Medio\"|\"Dificil\", \"caloriasEstimadas\": number, " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}], \"pasos\": [string]} | null, " +
        "\"ingredienteSustituir\": string | null, \"tituloEliminar\": string | null, \"eliminarTodas\": boolean, " +
        "\"tituloPlanificar\": string | null, \"fechaPlanificar\": string | null, \"turnoPlanificar\": string | null, " +
        "\"tituloCocinar\": string | null, \"porcionesCocinar\": number | null}\n\n" +
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
        "- 'cocinar_receta': el usuario avisa que YA cocinó/preparó una receta y quiere descontar los " +
        "ingredientes usados de su alacena (ej. 'ya la hice', 'cociné la tortilla', 'preparé esta receta', " +
        "'descontá los ingredientes', 'ya la cociné, bajá el stock'). En 'tituloCocinar' poné el nombre de la receta " +
        "(o dejalo null si se refiere a la última receta de la conversación). En 'porcionesCocinar' poné cuántas " +
        "porciones cocinó si lo aclara (ej. 'hice 2 porciones'); si no lo aclara, dejalo null (se asume la receta completa). " +
        "En 'mensaje' confirmás. El sistema descuenta del stock lo que haya y avisa si faltó algo.\n" +
        "Nunca inventes que guardaste, borraste, planificaste o cocinaste algo si la acción no fue la correspondiente.";

    private const string NombrePorDefecto = "Chef";

    // Memoria liviana en proceso: guardamos la ÚLTIMA receta que el agente le generó a cada
    // usuario. Así, cuando pide "guardala" en otro mensaje, no necesitamos que el cliente
    // reenvíe la receta (clave para probar desde Swagger). Se reinicia si se reinicia la API.
    private static readonly ConcurrentDictionary<int, RecetaGeneradaDto> UltimaRecetaPorUsuario = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAsistenteIaClient _asistenteIa;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRecetaIaService _recetaIaService;
    private readonly ISustitucionService _sustitucionService;
    private readonly IRecomendacionService _recomendacionService;
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<RecetaFavorita> _favoritoRepository;
    private readonly IPlanificadorService _planificadorService;
    private readonly IPreparacionService _preparacionService;

    public ChatIaService(
        IAsistenteIaClient asistenteIa,
        IRepository<Usuario> usuarioRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRecetaIaService recetaIaService,
        ISustitucionService sustitucionService,
        IRecomendacionService recomendacionService,
        IRepository<Receta> recetaRepository,
        IRepository<RecetaFavorita> favoritoRepository,
        IPlanificadorService planificadorService,
        IPreparacionService preparacionService)
    {
        _asistenteIa = asistenteIa;
        _usuarioRepository = usuarioRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
        _recetaIaService = recetaIaService;
        _sustitucionService = sustitucionService;
        _recomendacionService = recomendacionService;
        _recetaRepository = recetaRepository;
        _favoritoRepository = favoritoRepository;
        _planificadorService = planificadorService;
        _preparacionService = preparacionService;
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
        //    Si la API de IA falla (típico: 429 por límite de uso de la cuota), devolvemos
        //    un mensaje claro en vez de un 500 "error desconocido".
        string json;
        try
        {
            json = await _asistenteIa.GenerarRespuestaConversacionAsync(mensajes, systemInstruction, jsonMode: true);
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

        // 4. Ejecutamos el efecto según la acción que decidió el agente.
        return (sobre.Accion ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            ChatAccion.GenerarReceta => await ResolverGenerarRecetaAsync(usuarioId, sobre),
            ChatAccion.GuardarReceta => await ResolverGuardarRecetaAsync(usuarioId, sobre, request.RecetaActual),
            ChatAccion.Sustituir => await ResolverSustituirAsync(usuarioId, sobre),
            ChatAccion.Recomendar => await ResolverRecomendarAsync(usuarioId, sobre),
            ChatAccion.EliminarReceta => await ResolverEliminarRecetaAsync(usuarioId, sobre),
            ChatAccion.PlanificarReceta => await ResolverPlanificarRecetaAsync(usuarioId, sobre),
            ChatAccion.CocinarReceta => await ResolverCocinarRecetaAsync(usuarioId, sobre),
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

        // Si el agente no puso un título válido, generamos uno descriptivo (nunca "string").
        receta.Titulo = RecetaIaService.GenerarTituloPorDefecto(receta.Titulo, receta.Ingredientes);

        // Recordamos esta receta para este usuario: si luego dice "guardala", la usamos
        // aunque el cliente no la reenvíe.
        UltimaRecetaPorUsuario[usuarioId] = receta;

        // Damos de alta en el catálogo los ingredientes usados, pero "best-effort":
        // generar es un borrador, no debe romperse si el alta de catálogo falla. El catálogo
        // se completa igual cuando el usuario guarda la receta (ahí sí se persiste todo).
        try
        {
            await _recetaIaService.AsegurarIngredientesEnCatalogoAsync(receta);
        }
        catch
        {
            // Ignoramos el error a propósito: el borrador igual se devuelve.
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
        // Elegimos la mejor fuente de la receta, en orden de confianza:
        // 1) la que repitió la IA en el sobre, 2) la que el cliente mandó en pantalla,
        // 3) la última que le generamos a este usuario (memoria del servidor).
        // Descartamos las que vengan vacías o con el placeholder "string".
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

            // Ya quedó guardada: limpiamos la memoria para no re-guardarla por error.
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

    // Una receta sirve para guardar si tiene ingredientes y un título de verdad
    // (no vacío ni el placeholder "string" que mete Swagger por defecto).
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

        // Si no pidió borrar todo y tampoco dio un título, le preguntamos.
        if (!sobre.EliminarTodas && string.IsNullOrWhiteSpace(titulo))
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "¿Querés borrar una receta puntual (decime el nombre) o todas tus recetas guardadas?"
            };
        }

        // Solo tocamos recetas que el usuario tenga guardadas en favoritos: así nadie
        // puede eliminar recetas de otros usuarios desde el chat.
        var favoritos = await _favoritoRepository.FindWithIncludesAsync(
            favorito => favorito.UsuarioId == usuarioId,
            favorito => favorito.Receta);

        // Borrado en masa: todas las recetas guardadas del usuario.
        var recetas = favoritos
            .Select(favorito => favorito.Receta)
            .Where(receta => receta is not null)
            .GroupBy(receta => receta!.Id)
            .Select(grupo => grupo.First()!)
            .ToList();

        // Borrado puntual: filtramos por coincidencia flexible del título (sin comillas, sin
        // distinguir mayúsculas, y aceptando coincidencias parciales en cualquier dirección).
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

        // Borrar la receta arrastra en cascada sus ingredientes, pasos, favoritos y planificación.
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
        // 1. Resolvemos qué receta planificar y nos aseguramos de tener su id real (guardada).
        var (recetaId, tituloReceta, errorReceta) = await ResolverRecetaParaPlanificarAsync(usuarioId, sobre);
        if (recetaId is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = errorReceta!
            };
        }

        // 2. Fecha: si la IA mandó una (YYYY-MM-DD), la usamos; si no, hoy.
        var fecha = ParsearFecha(sobre.FechaPlanificar);

        // 3. Turno: el que indicó la IA o "Almuerzo" por defecto.
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

            // Armamos el aviso: qué se planificó y qué se sumó a la lista de compras.
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
            // Choca con otra comida en esa fecha/turno.
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = ex.Message
            };
        }
    }

    // Devuelve el id de la receta a planificar (debe estar guardada para tener id).
    // Busca primero entre los favoritos por título; si no, guarda la última receta recordada.
    private async Task<(int? recetaId, string? titulo, string? error)> ResolverRecetaParaPlanificarAsync(
        int usuarioId,
        SobreAgente sobre)
    {
        var titulo = sobre.TituloPlanificar?.Trim();

        // a) Si dio un título, lo buscamos entre sus recetas guardadas.
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

        // b) Si no la encontró guardada, usamos la última receta generada (memoria) y la guardamos
        //    para obtener un id real con el que planificar.
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

    private async Task<ChatRespuestaDto> ResolverCocinarRecetaAsync(int usuarioId, SobreAgente sobre)
    {
        // 1. Resolvemos qué receta cocinó y conseguimos su id real (tiene que estar guardada).
        var (recetaId, tituloReceta, error) = await ResolverRecetaParaCocinarAsync(usuarioId, sobre);
        if (recetaId is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = error!
            };
        }

        // 2. Porciones cocinadas: las que aclaró el usuario o 0 = receta completa (porciones base).
        var porciones = sobre.PorcionesCocinar is > 0 ? sobre.PorcionesCocinar.Value : 0;

        try
        {
            var resultado = await _preparacionService.DescontarIngredientesParcialAsync(usuarioId, recetaId.Value, porciones);

            var descontado = resultado.Descontados
                .Select(ingrediente => $"{ingrediente.Nombre}: -{ingrediente.Cantidad} {ingrediente.UnidadMedida}")
                .ToList();
            var faltante = resultado.Faltantes
                .Select(ingrediente => $"{ingrediente.Nombre}: faltaron {ingrediente.Cantidad} {ingrediente.UnidadMedida}")
                .ToList();

            var aviso = descontado.Count > 0
                ? $" Desconté de tu alacena: {string.Join(", ", descontado)}."
                : " No tenías stock cargado de los ingredientes, así que no descontué nada.";
            if (faltante.Count > 0)
            {
                aviso += $" Ojo, no te alcanzaba para: {string.Join(", ", faltante)}.";
            }

            return new ChatRespuestaDto
            {
                Accion = ChatAccion.CocinarReceta,
                Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                    ? $"Listo, registré que cocinaste \"{tituloReceta}\"." + aviso
                    : sobre.Mensaje + aviso,
                StockDescontado = descontado,
                StockFaltante = faltante
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = $"No pude descontar el stock: {ex.Message}"
            };
        }
    }

    // Devuelve el id de la receta que el usuario cocinó (debe estar guardada para tener id).
    // Busca primero entre los favoritos por título; si no, guarda la última receta recordada.
    private async Task<(int? recetaId, string? titulo, string? error)> ResolverRecetaParaCocinarAsync(
        int usuarioId,
        SobreAgente sobre)
    {
        var titulo = sobre.TituloCocinar?.Trim();

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
                return (null, null, $"No pude preparar la receta para descontar el stock: {ex.Message}");
            }
        }

        return (null, null, "No sé qué receta cocinaste. Generá o guardá una receta primero, " +
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

    // Coincidencia tolerante para que funcione sin comillas: ignora mayúsculas/espacios
    // y acepta que el término sea parte del título o viceversa (ej. "tortilla" ~ "Tortilla de papas").
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

        // Preferimos la receta que mandó el cliente; si no es válida (vacía o "string"),
        // usamos la última que le generamos a este usuario (memoria del servidor).
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

        [JsonPropertyName("eliminarTodas")]
        public bool EliminarTodas { get; set; }

        [JsonPropertyName("tituloPlanificar")]
        public string? TituloPlanificar { get; set; }

        [JsonPropertyName("fechaPlanificar")]
        public string? FechaPlanificar { get; set; }

        [JsonPropertyName("turnoPlanificar")]
        public string? TurnoPlanificar { get; set; }

        [JsonPropertyName("tituloCocinar")]
        public string? TituloCocinar { get; set; }

        [JsonPropertyName("porcionesCocinar")]
        public int? PorcionesCocinar { get; set; }
    }
}
