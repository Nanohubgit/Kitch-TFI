using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Kitch.Application.Common;
using Kitch.Application.DTOs.ChatIa;
using Kitch.Application.DTOs.Planificador;
using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.DTOs.Recomendacion;
using Kitch.Application.DTOs.StockUsuarios;
using Kitch.Application.DTOs.Sustituciones;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Microsoft.Extensions.Caching.Memory;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ChatIaService : IChatIaService
{
    private const string InstruccionAgente =
        "__RESTRICCION_DIETETICA__\n\n" +
        "Sos 'Kitch-AI', el asistente de cocina exclusivo de la plataforma Kitch. Estás hablando con __NOMBRE_USUARIO__. " +
        "Únicamente respondés temas de cocina: recetas, ingredientes, técnicas culinarias, pastelería y planificación de comidas. " +
        "Entendé errores de tipeo y formas coloquiales (ej. '¿cómo puedo hacer un milhojas gourmet desde 0?', 'como hjacer...', 'cómo se prepara...', 'quiero hacer...'): " +
        "eso SIEMPRE es un pedido válido de cocina y NUNCA debés rechazarlo diciendo que no podés ayudar. " +
        "Solo si te preguntan algo completamente ajeno a la cocina (programación, política, deportes, etc.), rechazalo con amabilidad (accion 'conversar'). " +
        "Conocés la alacena del usuario (te la pasamos como contexto) y la usás para recomendar y avisar qué le falta. " +
        "Toda receta, sustitución o recomendación DEBE respetar la restricción dietética del bloque anterior.\n\n" +
        "MEMORIA: tenés el historial de esta conversación y, en el contexto, la lista 'RECETAS RECIENTES' numerada " +
        "(1 = la más reciente). Entendé referencias temporales e implícitas: 'la anterior', 'la última', 'esa', 'guardala' → 1; " +
        "'la anteúltima', 'la de antes', 'la primera que me diste' → el número que corresponda; 'la de fideos con papa' → la que tenga ese plato. " +
        "Cuando el usuario se refiera a una receta de esa lista, poné su número en 'indiceReceta' y su título en 'nombre'. " +
        "Si hay varias recetas, identificá CUÁL pidió el usuario; no inventes otra.\n\n" +
        "RESPONDÉS SIEMPRE EXCLUSIVAMENTE con un ÚNICO objeto JSON válido, sin texto extra ni markdown, con esta forma EXACTA:\n" +
        "{\"accion\": \"conversar\"|\"generar_receta\"|\"guardar_receta\"|\"sustituir\"|\"recomendar\"|\"eliminar_receta\"|\"planificar_receta\"|\"eliminar_comida_planificada\"|\"cocinar_receta\"|\"consultar_recetas_guardadas\"|\"agregar_ingrediente_alacena\"|\"actualizar_ingrediente_alacena\"|\"eliminar_ingrediente_alacena\", " +
        "\"mensaje\": string, " +
        "\"nombre\": string | null, " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}] | null, " +
        "\"pasos\": [string] | null, " +
        "\"receta\": {\"basesYaUsadas\": [string], \"platoClasico\": string, \"ingredientesDescartados\": [string], \"ingredientesAComprar\": [string], " +
        "\"titulo\": string, \"descripcion\": string, \"tiempoPreparacionMinutos\": number, " +
        "\"porciones\": number, \"dificultad\": \"Facil\"|\"Medio\"|\"Dificil\", \"caloriasEstimadas\": number, " +
        "\"categoria\": \"pastas\"|\"carnes\"|\"pollo\"|\"ensaladas\"|\"sopas\"|\"pescados\"|\"pizzas\"|\"postres\"|\"tartas\"|\"guisos\"|\"general\", " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}], \"pasos\": [string]} | null, " +
        "\"ingredienteSustituir\": string | null, \"tituloEliminar\": string | null, \"eliminarTodas\": boolean, " +
        "\"tituloPlanificar\": string | null, \"fechaPlanificar\": string | null, \"turnoPlanificar\": string | null, " +
        "\"tituloCocinar\": string | null, \"porcionesCocinar\": number | null, " +
        "\"ingredientesAlacena\": [{\"nombre\": string, \"cantidad\": number | null, \"unidadMedida\": string | null}] | null, " +
        "\"eliminarTodosAlacena\": boolean, " +
        "\"indiceReceta\": number | null, \"reemplazarSiConflicto\": boolean}\n\n" +
        "REGLAS DE LA 'accion':\n" +
        "- 'conversar': charla general, dudas, consejos. 'receta' va en null.\n" +
        "- 'generar_receta': el usuario pide una receta, pregunta cómo hacer/preparar un plato, pide ideas de qué cocinar o qué puede hacer con lo que tiene " +
        "(ej. '¿qué puedo cocinar con lo que tengo?', '¿cómo puedo hacer un milhojas gourmet desde 0?', 'cómo se hace un risotto', 'receta rápida para hoy', 'generame algo con lo de la alacena', " +
        "'tengo ganas de algo con huevo para la cena', 'dame una receta de carne al horno', 'dame otra'). Ante la duda entre 'conversar' y 'generar_receta' " +
        "cuando el usuario quiere cocinar o saber cómo preparar un plato, usá SIEMPRE 'generar_receta'. OBLIGATORIO: completá el objeto 'receta' (NUNCA lo dejes null; " +
        "mín. 1 ingrediente y 1 paso, tiempo y porciones > 0, y poné 'ingredientes' y 'pasos' DENTRO de 'receta'). " +
        "Si el usuario pide un plato o ingrediente puntual (ej. carne, pollo, polenta, milhojas gourmet desde 0), generá la receta aunque no tenga esos ingredientes en la alacena " +
        "(el sistema le muestra qué le falta comprar), salvo que viole su restricción dietética. " +
        "Si no pidió un plato puntual, basala en los ingredientes de la alacena que combinen bien, siguiendo las reglas de " +
        "REALISMO CULINARIO. Una receta nueva NUNCA copia una de RECETAS RECIENTES ni la receta en pantalla: " +
        "pensala de cero aunque los ingredientes sean los mismos, salvo que el usuario pida explícitamente repetirla. " +
        "En 'mensaje' escribí SOLO una presentación breve de 1 o 2 oraciones (máximo 160 caracteres) presentando la receta. " +
        "PROHIBIDO listar ingredientes, cantidades, pasos numerados ni instrucciones de preparación dentro de 'mensaje': todo eso va únicamente en el objeto 'receta' porque la app lo muestra en una tarjeta desplegable. " +
        "SIEMPRE poné en 'titulo' el nombre real y reconocible del plato (ej. 'Tortilla de papas'); NUNCA uses 'string', " +
        "'receta' ni dejes el título vacío. " +
        "En 'ingredientes[].nombre' poné solo el nombre limpio del ingrediente, NUNCA '(opcional)', 'opcional' ni paréntesis " +
        "(si algo es opcional, aclaralo solo en 'descripcion'). " +
        "OBLIGATORIO: en 'categoria' elegí exactamente uno de estos valores en minúsculas: " +
        "pastas, carnes, pollo, ensaladas, sopas, pescados, pizzas, postres, tartas, guisos, general. " +
        "Si no encaja con claridad, usá 'general'.\n" +
        "- 'guardar_receta': el usuario pide guardar una receta ('guardame la receta anterior', 'guardala', 'guarda la de fideos con papa'). " +
        "Si está en RECETAS RECIENTES, respondé {\"accion\":\"guardar_receta\",\"indiceReceta\":2,\"nombre\":\"Fideos con papa\",\"mensaje\":\"...\"} " +
        "sin copiar ingredientes ni pasos: el sistema usa la receta exacta que tiene guardada. " +
        "Solo si NO está en esa lista, completá nombre, ingredientes y pasos copiados del historial. " +
        "NUNCA uses 'conversar' para un pedido de guardado. No inventes una receta nueva al guardar.\n" +
        "- 'sustituir': el usuario pregunta con qué reemplazar un ingrediente. Poné el nombre del ingrediente " +
        "original en 'ingredienteSustituir'. En 'mensaje' explicás brevemente; el sistema completa la lista de sustitutos.\n" +
        "- 'recomendar': SOLO cuando el usuario pide explícitamente recetas del catálogo de Kitch o ya cargadas en la app " +
        "(ej. '¿qué recetas del catálogo puedo hacer?', 'mostrame recetas de la app con lo que tengo'). " +
        "'receta' va en null; el sistema agrega las recetas compatibles. En 'mensaje' introducís las recomendaciones.\n" +
        "- 'eliminar_receta': el usuario quiere borrar/eliminar recetas guardadas. " +
        "Si menciona UNA receta puntual (ej. 'borrá la de tortilla', 'eliminá la tarta de manzana'), poné solo el nombre " +
        "(sin comillas, tal como lo dijo) en 'tituloEliminar' y dejá 'eliminarTodas' en false. " +
        "Si quiere borrar TODAS (ej. 'borrá todas mis recetas', 'eliminá todos mis favoritos', 'borrá todo', 'limpiá mis favoritos'), " +
        "poné 'eliminarTodas' en true y 'tituloEliminar' en null. En 'mensaje' confirmás. " +
        "El sistema borra solo recetas que el usuario tenga guardadas.\n" +
        "- 'planificar_receta': el usuario quiere agendar/planificar una receta para una fecha y momento del día, o está respondiendo " +
        "el día/turno/confirmación que le pediste en el turno anterior para terminar de planificarla " +
        "(ej. 'planificá el guiso de lentejas para el domingo', 'Almuerzo', 'el viernes a la cena', 'sí, reemplazala'). " +
        "En 'tituloPlanificar' poné el nombre EXACTO de la receta mencionada en el pedido o en el historial de esta planificación " +
        "(revisá MIS RECETAS GUARDADAS y RECETAS RECIENTES). Si el usuario nombró una receta específica, dejá 'indiceReceta' en null " +
        "para no confundirla con otra receta reciente; usá 'indiceReceta' SOLO si dijo 'esta', 'la última', 'la anterior' o un número. " +
        "Si el sistema avisó que ese turno ya estaba ocupado y el usuario confirma ('sí, reemplazala'), repetí receta, fecha y turno y poné 'reemplazarSiConflicto' en true. " +
        "En 'fechaPlanificar' poné YYYY-MM-DD (usando 'Fecha de hoy' del contexto para resolver expresiones como 'hoy', 'mañana', 'pasado mañana', 'el día 25', 'el 25', '25/11' o días de la semana, y conservando la fecha que el usuario dijo en turnos previos de esta planificación si ahora solo indicó el turno). " +
        "En 'turnoPlanificar' poné 'Desayuno', 'Almuerzo', 'Merienda' o 'Cena' SOLO si el usuario lo dijo en esta planificación (no inventes un turno). " +
        "Usá 'planificar_receta' incluso si falta solo el turno o solo la fecha: el sistema le pregunta exactamente lo que falta.\n" +
        "- 'eliminar_comida_planificada': el usuario quiere quitar/sacar/borrar una o todas las comidas de su planificador semanal " +
        "(ej. 'sacá el almuerzo del domingo', 'borrá las hamburguesas de lentejas del planificador', 'limpiá mi planificador'). " +
        "Completá 'tituloPlanificar', 'fechaPlanificar' y/o 'turnoPlanificar' según lo que haya dicho, o 'eliminarTodas': true si quiere vaciarlo.\n" +
        "- 'agregar_ingrediente_alacena': el usuario quiere agregar uno o más ingredientes/alimentos a su alacena " +
        "(ej. 'agregá 6 huevos a mi alacena', 'compré 1 kg de papa y 1 litro de leche'). " +
        "Completá 'ingredientesAlacena' con nombre, cantidad y unidadMedida (si no indicó cantidad, usá 1 y unidad 'u').\n" +
        "- 'actualizar_ingrediente_alacena': el usuario quiere cambiar la cantidad de un ingrediente que ya tiene en su alacena " +
        "(ej. 'dejame 2 huevos en la alacena', 'me quedaron 300 gr de arroz'). Completá 'ingredientesAlacena'.\n" +
        "- 'eliminar_ingrediente_alacena': el usuario quiere borrar/eliminar/sacar uno, varios o todos los ingredientes de su alacena " +
        "(ej. 'borrá los huevooo de la alacena', 'eliminá el ajo y la leche', 'sacá la polenta de mi alacena', 'vaciá mi alacena'). " +
        "Entendé errores de tipeo, mayúsculas, plurales o letras repetidas y poné en 'ingredientesAlacena' el nombre tal como está en la alacena o como lo dijo el usuario; " +
        "si quiere borrar todo el stock, poné 'eliminarTodosAlacena': true.\n" +
        "- Si el usuario pide algo dulce (torta, postre, budín, flan) y no hay coincidencias, no recomiendes platos salados. " +
        "Generá o recomendá solo postres. Si pide algo salado, no recomiendes postres.\n" +
        "- 'cocinar_receta': el usuario avisa que YA cocinó/preparó una receta y quiere descontar los " +
        "ingredientes usados de su alacena (ej. 'ya la hice', 'cociné la tortilla', 'preparé esta receta', " +
        "'descontá los ingredientes', 'ya la cociné, bajá el stock'). En 'tituloCocinar' poné el nombre de la receta " +
        "(o dejalo null si se refiere a la última receta de la conversación; si es una de RECETAS RECIENTES, poné su número en 'indiceReceta'). " +
        "En 'porcionesCocinar' poné cuántas " +
        "porciones cocinó si lo aclara (ej. 'hice 2 porciones'); si no lo aclara, dejalo null (se asume la receta completa). " +
        "En 'mensaje' confirmás. El sistema descuenta del stock lo que haya y avisa si faltó algo.\n" +
        "- 'consultar_recetas_guardadas': el usuario pregunta cuántas recetas guardadas/favoritas tiene " +
        "(ej. 'cuántas recetas tengo', 'tengo favoritas?', 'decime mis recetas guardadas'). " +
        "No inventes cantidades: esta acción obliga al sistema a leer la base real. En 'mensaje' respondé en tono breve.\n" +
        "Nunca inventes que guardaste, borraste, planificaste o cocinaste algo si la acción no fue la correspondiente.";

    private const string NombrePorDefecto = "Chef";

    private const string PedidoRecetaSinCoincidencias =
        "No hay recetas del catálogo que pueda hacer con mi alacena. Generame una receta nueva, real y con lógica, " +
        "basada en los ingredientes de mi alacena que combinen bien entre sí (no hace falta usarlos todos). " +
        "Respondé con accion 'generar_receta'.";

    private const string MensajeRecetaSinCoincidencias =
        "No encontré recetas del catálogo que puedas hacer con lo que tenés, así que te armé una nueva con tu alacena.";

    private const string PedidoRecetaDulce =
        "El pedido es DULCE (torta, postre o similar). Generame solo un postre real, con categoria 'postres'. " +
        "PROHIBIDO devolver guiso, carne, ensalada, sopa u otro plato salado. Respondé con accion 'generar_receta'.";

    private const string PedidoRecetaSalada =
        "El pedido es SALADO. Generame un plato salado real. PROHIBIDO devolver torta, postre o algo dulce. " +
        "Respondé con accion 'generar_receta'.";

    private const string PedidoRecetaAlacenaVacia =
        "Mi alacena está vacía. Generame una receta sencilla, real y con pocos ingredientes básicos fáciles de conseguir. " +
        "Respondé con accion 'generar_receta'.";

    private const string MensajeRecetaAlacenaVacia =
        "Tu alacena está vacía, así que te propongo una receta sencilla con ingredientes básicos. " +
        "Guardala para ver qué te falta y tildar lo que ya tengas.";

    private const int MaxRecetasRecientes = 5;

    private const int MaxRecetasGuardadasEnContexto = 8;

    private static readonly JsonSerializerOptions JsonOptions = JsonIa.Opciones;

    private readonly IAsistenteIaClient _asistenteIa;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRecetaIaService _recetaIaService;
    private readonly ISustitucionService _sustitucionService;
    private readonly IRecomendacionService _recomendacionService;
    private readonly IRepository<RecetaFavorita> _favoritoRepository;
    private readonly IPlanificadorService _planificadorService;
    private readonly IPreparacionService _preparacionService;
    private readonly IIngredienteNormalizerService _normalizer;
    private readonly IFavoritoService _favoritoService;
    private readonly ICuotaIaService _cuotaIa;
    private readonly IRecetaService _recetaService;
    private readonly IStockUsuarioService _stockUsuarioService;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMemoryCache _cache;

    public ChatIaService(
        IAsistenteIaClient asistenteIa,
        IRepository<Usuario> usuarioRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRecetaIaService recetaIaService,
        ISustitucionService sustitucionService,
        IRecomendacionService recomendacionService,
        IRepository<RecetaFavorita> favoritoRepository,
        IPlanificadorService planificadorService,
        IPreparacionService preparacionService,
        IIngredienteNormalizerService normalizer,
        IFavoritoService favoritoService,
        ICuotaIaService cuotaIa,
        IRecetaService recetaService,
        IStockUsuarioService stockUsuarioService,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMemoryCache cache)
    {
        _recetaService = recetaService;
        _stockUsuarioService = stockUsuarioService;
        _unidadDeTrabajo = unidadDeTrabajo;
        _cache = cache;
        _asistenteIa = asistenteIa;
        _usuarioRepository = usuarioRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
        _recetaIaService = recetaIaService;
        _sustitucionService = sustitucionService;
        _recomendacionService = recomendacionService;
        _favoritoRepository = favoritoRepository;
        _planificadorService = planificadorService;
        _preparacionService = preparacionService;
        _normalizer = normalizer;
        _favoritoService = favoritoService;
        _cuotaIa = cuotaIa;
    }

    public async Task<ChatRespuestaDto> ProcesarMensajeAsync(int usuarioId, ChatRequestDto request)
    {
        var turnos = ObtenerTurnos(request);
        if (turnos.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "Escribime algo para poder ayudarte con tu cocina."
            };
        }

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        var nombreUsuario = string.IsNullOrWhiteSpace(usuario?.Nombre) ? NombrePorDefecto : usuario!.Nombre;
        var restriccion = RestriccionDieteticaPrompt.ParaSystemPrompt(usuario?.PreferenciaDietetica);

        var systemInstruction = InstruccionAgente
            .Replace("__RESTRICCION_DIETETICA__", restriccion)
            .Replace("__NOMBRE_USUARIO__", nombreUsuario) +
            "\n\n" + RealismoRecetaPrompt.Instruccion;

        if (!RolUsuario.TieneAccesoPremium(usuario?.Rol))
        {
            systemInstruction +=
                "\n\n" + RecetaIaService.InstruccionPlanBasico +
                " Si el plato pedido solo puede salir difícil, usá 'conversar', NUNCA respondas simplemente 'no puedo ayudarte': " +
                "explicá con claridad que las recetas difíciles son del plan Profesional y ofrecé una alternativa realmente simple.";
        }

        var ultimoTexto = turnos[^1].Texto;
        if (!RolUsuario.TieneAccesoPremium(usuario?.Rol) && PedidoRecetaPremium.EsPedido(ultimoTexto))
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = LimitesPlan.MensajePedidoDificilBasico
            };
        }

        try
        {
            await _cuotaIa.ConsumirAsync(usuarioId);
        }
        catch (ForbiddenException ex)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = ex.Message
            };
        }

        var contexto = await ConstruirContextoAsync(usuarioId, request.RecetaActual, usuario?.PreferenciaDietetica);
        var mensajes = ConstruirConversacion(contexto, turnos);

        string json;
        try
        {
            json = await _asistenteIa.GenerarRespuestaConversacionAsync(mensajes, systemInstruction, jsonMode: true);
        }
        catch (ServicioIaException ex) when (ex.Motivo == MotivoFallaIa.NoConfigurado)
        {
            await _cuotaIa.DevolverAsync(usuarioId);
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "El asistente de IA no está configurado en el servidor. Avisale al administrador de Kitch."
            };
        }
        catch (ServicioIaException ex) when (ex.Motivo == MotivoFallaIa.Saturado)
        {
            await _cuotaIa.DevolverAsync(usuarioId);
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "El asistente está recibiendo demasiadas consultas en este momento " +
                    "(se alcanzó el límite de uso de la API de IA). Esperá unos segundos y volvé a intentar."
            };
        }
        catch (ServicioIaException ex) when (ex.Motivo == MotivoFallaIa.NoDisponible)
        {
            await _cuotaIa.DevolverAsync(usuarioId);
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "El asistente de IA está sobrecargado en este momento (problema temporal del proveedor). " +
                    "Esperá unos segundos y volvé a intentar."
            };
        }
        catch (ServicioExternoException)
        {
            await _cuotaIa.DevolverAsync(usuarioId);
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No pude conectarme con el asistente de IA en este momento. Probá de nuevo en unos instantes."
            };
        }

        var sobre = DeserializarSobre(json);

        if (sobre is null)
        {
            await _cuotaIa.DevolverAsync(usuarioId);
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "Perdón, no pude procesar la respuesta. ¿Podés reformular tu pedido?"
            };
        }

        sobre = await RecuperarRechazoFalsoRecetaAsync(sobre, mensajes, systemInstruction, ultimoTexto);

        if (string.Equals(sobre.Accion?.Trim(), ChatAccion.GenerarReceta, StringComparison.OrdinalIgnoreCase))
        {
            sobre = await AsegurarRecetaCompletaAsync(sobre, mensajes, systemInstruction, ultimoTexto);
            sobre = await EvitarRecetaRepetidaAsync(usuarioId, sobre, mensajes, systemInstruction, ultimoTexto);
            sobre = await EvitarPostreFalsoAsync(sobre, mensajes, systemInstruction, ultimoTexto);
        }

        var respuesta = (sobre.Accion ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            ChatAccion.GenerarReceta => await ResolverGenerarRecetaAsync(usuarioId, sobre),
            ChatAccion.GuardarReceta => await ResolverGuardarRecetaAsync(usuarioId, sobre, request.RecetaActual),
            ChatAccion.Sustituir => await ResolverSustituirAsync(usuarioId, sobre),
            ChatAccion.Recomendar => await ResolverRecomendarAsync(usuarioId, sobre, mensajes, systemInstruction, ultimoTexto),
            ChatAccion.EliminarReceta => await ResolverEliminarRecetaAsync(usuarioId, sobre),
            ChatAccion.PlanificarReceta => await ResolverPlanificarRecetaAsync(usuarioId, sobre, turnos),
            ChatAccion.EliminarComidaPlanificada => await ResolverEliminarComidaPlanificadaAsync(usuarioId, sobre, turnos),
            ChatAccion.CocinarReceta => await ResolverCocinarRecetaAsync(usuarioId, sobre),
            ChatAccion.ConsultarRecetasGuardadas => await ResolverConsultarRecetasGuardadasAsync(usuarioId, sobre),
            ChatAccion.AgregarIngredienteAlacena => await ResolverAgregarIngredienteAlacenaAsync(usuarioId, sobre),
            ChatAccion.ActualizarIngredienteAlacena => await ResolverActualizarIngredienteAlacenaAsync(usuarioId, sobre),
            ChatAccion.EliminarIngredienteAlacena => await ResolverEliminarIngredienteAlacenaAsync(usuarioId, sobre, ultimoTexto),
            _ => new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                    ? "Contame qué querés cocinar y te ayudo."
                    : sobre.Mensaje
            }
        };

        respuesta.RecetasRecientes = (await ObtenerRecetasRecientesAsync(usuarioId))
            .Select((receta, indice) => new RecetaRecienteDto
            {
                Indice = indice + 1,
                Titulo = receta.Titulo,
                Dificultad = receta.Dificultad,
                Categoria = receta.Categoria
            })
            .ToList();

        return respuesta;
    }

    private async Task<SobreAgente> RecuperarRechazoFalsoRecetaAsync(
        SobreAgente sobre,
        List<MensajeIa> mensajes,
        string systemInstruction,
        string pedidoUsuario)
    {
        if (!string.Equals(sobre.Accion?.Trim(), ChatAccion.Conversar, StringComparison.OrdinalIgnoreCase))
        {
            return sobre;
        }

        if (!ParecePedidoDeReceta(pedidoUsuario) || !PareceRechazoGenerico(sobre.Mensaje))
        {
            return sobre;
        }

        var conversacion = new List<MensajeIa>(mensajes)
        {
            new("user",
                $"El pedido \"{pedidoUsuario}\" SÍ es sobre cocina y recetas. " +
                "No lo rechaces. Generá la receta completa con accion 'generar_receta', " +
                "completando 'receta' con todos sus ingredientes y pasos reales (aunque falten ingredientes en la alacena). " +
                "En 'mensaje' escribí solo 1 o 2 oraciones breves presentando el plato, sin listar ingredientes ni pasos.")
        };

        try
        {
            var json = await _asistenteIa.GenerarRespuestaConversacionAsync(conversacion, systemInstruction, jsonMode: true);
            var reintento = DeserializarSobre(json);
            if (reintento is not null)
            {
                var recetaReintento = ComponerRecetaDesdeSobre(reintento);
                if (EsRecetaValida(recetaReintento))
                {
                    reintento.Accion = ChatAccion.GenerarReceta;
                    reintento.Receta = recetaReintento;
                    return reintento;
                }
            }
        }
        catch (ServicioExternoException)
        {
        }

        return sobre;
    }

    private static bool ParecePedidoDeReceta(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = QuitarTildes(texto.Trim().ToLowerInvariant());
        return Regex.IsMatch(
            limpio,
            @"\b(receta|cocinar|cocino|preparar|preparo|como\s+(?:puedo\s+|se\s+)?(?:h[a-z]{0,2}cer|hago|prepar|cocin|armar)|desde\s+(?:0|cero)|quiero\s+hacer|haceme|dame\s+un[ao]?\s+)\b",
            RegexOptions.CultureInvariant);
    }

    private static bool PareceRechazoGenerico(string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return false;
        }

        var limpio = QuitarTildes(mensaje.Trim().ToLowerInvariant());
        var esMotivoValido =
            limpio.Contains("profesional", StringComparison.Ordinal) ||
            limpio.Contains("basico", StringComparison.Ordinal) ||
            limpio.Contains("premium", StringComparison.Ordinal) ||
            limpio.Contains("celiac", StringComparison.Ordinal) ||
            limpio.Contains("vegan", StringComparison.Ordinal) ||
            limpio.Contains("vegetar", StringComparison.Ordinal) ||
            limpio.Contains("diabet", StringComparison.Ordinal) ||
            limpio.Contains("tacc", StringComparison.Ordinal) ||
            limpio.Contains("gluten", StringComparison.Ordinal) ||
            limpio.Contains("restriccion", StringComparison.Ordinal);

        if (esMotivoValido)
        {
            return false;
        }

        return limpio.Contains("no puedo ayudar", StringComparison.Ordinal) ||
               limpio.Contains("no puedo asistir", StringComparison.Ordinal) ||
               limpio.Contains("no puedo responder", StringComparison.Ordinal) ||
               limpio.Contains("no puedo generar", StringComparison.Ordinal) ||
               limpio.Contains("ajeno a la cocina", StringComparison.Ordinal) ||
               limpio.Contains("fuera de mi alcance", StringComparison.Ordinal) ||
               limpio.Contains("solo puedo ayudar", StringComparison.Ordinal) ||
               limpio.Contains("solo respondo temas de cocina", StringComparison.Ordinal);
    }

    private static string SanitizarMensajePresentacionReceta(string? mensajeIa, string fallback)
    {
        if (string.IsNullOrWhiteSpace(mensajeIa))
        {
            return fallback;
        }

        var sinMarkdown = mensajeIa
            .Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal)
            .Replace("###", string.Empty, StringComparison.Ordinal)
            .Replace("##", string.Empty, StringComparison.Ordinal)
            .Trim();

        var limpio = QuitarTildes(sinMarkdown.ToLowerInvariant());
        var tieneListaOPasos =
            limpio.Contains("ingredientes:", StringComparison.Ordinal) ||
            limpio.Contains("preparacion:", StringComparison.Ordinal) ||
            limpio.Contains("pasos:", StringComparison.Ordinal) ||
            limpio.Contains("instrucciones:", StringComparison.Ordinal) ||
            Regex.IsMatch(sinMarkdown, @"(?:^|\n)\s*(?:\d+[\.\)]\s+|[-*•]\s+)", RegexOptions.Multiline) ||
            Regex.IsMatch(sinMarkdown, @"\b1[\.\)]\s+.+\b2[\.\)]\s+", RegexOptions.Singleline) ||
            sinMarkdown.Contains('\n', StringComparison.Ordinal) ||
            sinMarkdown.Length > 220;

        if (!tieneListaOPasos)
        {
            return sinMarkdown;
        }

        var corte = sinMarkdown.Length;
        var indices = new[]
        {
            sinMarkdown.IndexOf('\n'),
            limpio.IndexOf("ingredientes", StringComparison.Ordinal),
            limpio.IndexOf("preparacion", StringComparison.Ordinal),
            limpio.IndexOf("pasos", StringComparison.Ordinal),
            limpio.IndexOf("instrucciones", StringComparison.Ordinal)
        };

        foreach (var indice in indices)
        {
            if (indice >= 0 && indice < corte)
            {
                corte = indice;
            }
        }

        var matchNumero = Regex.Match(sinMarkdown, @"\b1[\.\)]\s+");
        if (matchNumero.Success && matchNumero.Index < corte)
        {
            corte = matchNumero.Index;
        }

        var intro = sinMarkdown[..corte].Trim().TrimEnd(':', '-', '—', ';');
        if (intro.Length is >= 18 and <= 200)
        {
            if (!intro.EndsWith('.') && !intro.EndsWith('!') && !intro.EndsWith('?'))
            {
                intro += ".";
            }

            return intro;
        }

        return fallback;
    }

    private async Task<SobreAgente> AsegurarRecetaCompletaAsync(
        SobreAgente sobre,
        List<MensajeIa> mensajes,
        string systemInstruction,
        string pedidoUsuario)
    {
        var compuesta = ComponerRecetaDesdeSobre(sobre);
        if (EsRecetaValida(compuesta))
        {
            sobre.Receta = compuesta;
            return sobre;
        }

        var conversacion = new List<MensajeIa>(mensajes)
        {
            new("user",
                $"Respondiste con accion 'generar_receta' pero dejaste el objeto 'receta' vacío o sin ingredientes. " +
                $"Generá ahora una receta clásica real para: \"{pedidoUsuario}\". " +
                "Completá el objeto 'receta' con titulo, descripcion, tiempoPreparacionMinutos, porciones, dificultad, caloriasEstimadas, categoria, ingredientes y pasos, " +
                "aunque falten ingredientes en la alacena (anotalos en ingredientesAComprar). Respondé con accion 'generar_receta'.")
        };

        try
        {
            var json = await _asistenteIa.GenerarRespuestaConversacionAsync(conversacion, systemInstruction, jsonMode: true);
            var reintento = DeserializarSobre(json);
            if (reintento is not null)
            {
                var recetaReintento = ComponerRecetaDesdeSobre(reintento);
                if (EsRecetaValida(recetaReintento))
                {
                    reintento.Accion = ChatAccion.GenerarReceta;
                    reintento.Receta = recetaReintento;
                    return reintento;
                }
            }
        }
        catch (ServicioExternoException)
        {
        }

        return sobre;
    }

    private async Task<ChatRespuestaDto> ResolverGenerarRecetaAsync(int usuarioId, SobreAgente sobre)
    {
        var receta = ComponerRecetaDesdeSobre(sobre);
        if (receta is null || receta.Ingredientes.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No pude armar una receta válida esta vez. Probá pedírmela de nuevo o cargá más ingredientes en tu alacena."
            };
        }

        foreach (var ingrediente in receta.Ingredientes)
        {
            ingrediente.Nombre = _normalizer.LimpiarNombreVisible(ingrediente.Nombre);
        }

        receta.Titulo = RecetaIaService.GenerarTituloPorDefecto(receta.Titulo, receta.Ingredientes);
        var dificultad = RecetaIaService.NormalizarDificultad(receta);

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (!LimitesPlan.PuedeUsarDificultad(usuario?.Rol, dificultad))
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = LimitesPlan.MensajeRecetaGeneradaDificilBasico
            };
        }

        await RecordarRecetaAsync(usuarioId, receta);

        try
        {
            await _recetaIaService.AsegurarIngredientesEnCatalogoAsync(receta);
        }
        catch
        {
        }

        RecetaGuardadaResponse? guardada = null;
        try
        {
            guardada = await _recetaIaService.GuardarRecetaAsync(usuarioId, receta);
        }
        catch (ForbiddenException ex)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = ex.Message
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or ConflictException or ArgumentException)
        {
        }

        var avisoHistorial = guardada is null
            ? $"Te propongo {receta.Titulo}. Abrí la tarjeta para ver los ingredientes y el paso a paso."
            : $"Te propongo {receta.Titulo}. Quedó en Recetas y podés ver ingredientes y pasos abriendo la tarjeta.";

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.GenerarReceta,
            Mensaje = SanitizarMensajePresentacionReceta(sobre.Mensaje, avisoHistorial),
            Receta = receta,
            RecetaGuardada = guardada
        };
    }

    private async Task<ChatRespuestaDto> ResolverGuardarRecetaAsync(
        int usuarioId,
        SobreAgente sobre,
        RecetaGeneradaDto? recetaActual)
    {
        var receta = await ElegirRecetaParaGuardarAsync(usuarioId, sobre, recetaActual);

        if (receta is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No encuentro una receta para guardar. Primero pedime que te genere una y después decime que la guarde."
            };
        }

        foreach (var ingrediente in receta.Ingredientes)
        {
            ingrediente.Nombre = _normalizer.LimpiarNombreVisible(ingrediente.Nombre);
        }

        receta.Titulo = RecetaIaService.GenerarTituloPorDefecto(receta.Titulo, receta.Ingredientes);

        try
        {
            var guardada = await _recetaIaService.GuardarRecetaAsync(usuarioId, receta);

            await OlvidarRecetaAsync(usuarioId, receta.Titulo);
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.GuardarReceta,
                Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                    ? guardada.Mensaje
                    : sobre.Mensaje,
                Receta = receta,
                RecetaGuardada = guardada
            };
        }
        catch (ForbiddenException ex)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = string.IsNullOrWhiteSpace(ex.Message)
                    ? LimitesPlan.MensajeLimiteFavoritos
                    : ex.Message
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or ConflictException or ArgumentException)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = $"No pude guardar la receta: {ex.Message}"
            };
        }
    }

    private async Task<RecetaGeneradaDto?> ElegirRecetaParaGuardarAsync(
        int usuarioId,
        SobreAgente sobre,
        RecetaGeneradaDto? recetaActual)
    {
        var recientes = await ObtenerRecetasRecientesAsync(usuarioId);
        var titulo = string.IsNullOrWhiteSpace(sobre.Nombre) ? sobre.Receta?.Titulo : sobre.Nombre;

        var reciente = BuscarEnRecientes(recientes, sobre.IndiceReceta, titulo);
        if (reciente is not null)
        {
            return reciente;
        }

        var delSobre = ComponerRecetaDesdeSobre(sobre);
        if (EsRecetaValida(delSobre))
        {
            return delSobre;
        }

        if (EsRecetaValida(recetaActual))
        {
            return recetaActual;
        }

        return recientes.FirstOrDefault();
    }

    private static RecetaGeneradaDto? BuscarEnRecientes(
        IReadOnlyList<RecetaGeneradaDto> recientes,
        int? indice,
        string? titulo)
    {
        if (indice is >= 1 && indice <= recientes.Count)
        {
            return recientes[indice.Value - 1];
        }

        if (string.IsNullOrWhiteSpace(titulo))
        {
            return null;
        }

        return recientes.FirstOrDefault(receta => receta.Titulo.Trim().Equals(titulo.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? recientes.FirstOrDefault(receta => CoincideTitulo(receta.Titulo, titulo));
    }

    private async Task<List<RecetaGeneradaDto>> ObtenerRecetasRecientesAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        return LeerRecetasRecientes(usuario?.UltimaRecetaIaJson);
    }

    private static List<RecetaGeneradaDto> LeerRecetasRecientes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            if (json.TrimStart().StartsWith('['))
            {
                return (JsonSerializer.Deserialize<List<RecetaGeneradaDto>>(json, JsonOptions) ?? [])
                    .Where(EsRecetaValida)
                    .Take(MaxRecetasRecientes)
                    .ToList();
            }

            var unica = JsonSerializer.Deserialize<RecetaGeneradaDto>(json, JsonOptions);
            return EsRecetaValida(unica) ? [unica!] : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task GuardarRecetasRecientesAsync(int usuarioId, List<RecetaGeneradaDto> recetas)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is null)
        {
            return;
        }

        usuario.UltimaRecetaIaJson = recetas.Count == 0
            ? null
            : JsonSerializer.Serialize(recetas.Take(MaxRecetasRecientes).ToList(), JsonOptions);
        await _usuarioRepository.UpdateAsync(usuario);
    }

    private async Task RecordarRecetaAsync(int usuarioId, RecetaGeneradaDto receta)
    {
        var recientes = await ObtenerRecetasRecientesAsync(usuarioId);
        recientes.RemoveAll(existente => existente.Titulo.Trim().Equals(receta.Titulo.Trim(), StringComparison.OrdinalIgnoreCase));
        recientes.Insert(0, receta);
        await GuardarRecetasRecientesAsync(usuarioId, recientes);
    }

    private async Task OlvidarRecetaAsync(int usuarioId, string titulo)
    {
        var recientes = await ObtenerRecetasRecientesAsync(usuarioId);
        var eliminadas = recientes.RemoveAll(existente => existente.Titulo.Trim().Equals(titulo.Trim(), StringComparison.OrdinalIgnoreCase));
        if (eliminadas > 0)
        {
            await GuardarRecetasRecientesAsync(usuarioId, recientes);
        }
    }

    private static RecetaGeneradaDto? ComponerRecetaDesdeSobre(SobreAgente sobre)
    {
        var receta = sobre.Receta;

        if (receta is null &&
            (sobre.Ingredientes is { Count: > 0 } || sobre.Pasos is { Count: > 0 }))
        {
            receta = new RecetaGeneradaDto();
        }

        if (receta is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(receta.Titulo) && !string.IsNullOrWhiteSpace(sobre.Nombre))
        {
            receta.Titulo = sobre.Nombre.Trim();
        }

        if ((receta.Ingredientes is null || receta.Ingredientes.Count == 0) &&
            sobre.Ingredientes is { Count: > 0 })
        {
            receta.Ingredientes = sobre.Ingredientes;
        }

        if ((receta.Pasos is null || receta.Pasos.Count == 0) &&
            sobre.Pasos is { Count: > 0 })
        {
            receta.Pasos = sobre.Pasos;
        }

        receta.Ingredientes ??= [];
        receta.Pasos ??= [];
        receta.Categoria = CategoriasReceta.Normalizar(receta.Categoria);

        if (receta.Pasos.Count == 0 && receta.Ingredientes.Count > 0)
        {
            receta.Pasos.Add("Preparar según la receta conversada.");
        }

        return receta;
    }

    private static bool EsRecetaValida(RecetaGeneradaDto? receta)
    {
        if (receta is null || receta.Ingredientes is null || receta.Ingredientes.Count == 0)
        {
            return false;
        }

        var titulo = receta.Titulo?.Trim();
        return string.IsNullOrWhiteSpace(titulo) ||
            !titulo.Equals("string", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ChatRespuestaDto> ResolverSustituirAsync(int usuarioId, SobreAgente sobre)
    {
        if (string.IsNullOrWhiteSpace(sobre.IngredienteSustituir))
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "¿Qué ingrediente querés reemplazar?"
            };
        }

        var nombre = _normalizer.Normalizar(sobre.IngredienteSustituir);
        if (nombre.Length > 100)
        {
            nombre = nombre[..100].TrimEnd();
        }

        var ingrediente = await _ingredienteRepository.FirstOrDefaultAsync(i => i.Nombre == nombre);
        ingrediente ??= await _ingredienteRepository.AddAsync(new Ingrediente
        {
            Nombre = nombre,
            Categoria = "Varios"
        });

        List<SustitutoSugerido> sustitutos;
        try
        {
            sustitutos = (await _sustitucionService.BuscarSustitutosAsync(usuarioId, ingrediente.Id)).ToList();
        }
        catch (ServicioExternoException)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No pude buscar reemplazos en este momento. Probá de nuevo en unos instantes."
            };
        }
        catch (ForbiddenException ex)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = ex.Message
            };
        }
        var hayMasPremium = sustitutos.Any(sustituto => sustituto.HayMasConProfesional);
        var mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
            ? $"Estos son los reemplazos que te recomiendo para {nombre}:"
            : sobre.Mensaje;
        if (hayMasPremium)
        {
            mensaje = $"{mensaje} {LimitesPlan.MensajeMasSustitutosPremium}";
        }

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.Sustituir,
            Mensaje = mensaje,
            IngredienteSustituido = ingrediente.Nombre,
            Sustitutos = sustitutos,
            HayMasSustitutosPremium = hayMasPremium
        };
    }

    private async Task<ChatRespuestaDto> ResolverRecomendarAsync(
        int usuarioId,
        SobreAgente sobre,
        List<MensajeIa> mensajes,
        string systemInstruction,
        string pedidoUsuario)
    {
        var sabor = DetectarSabor(pedidoUsuario);
        var candidatas = (await _recomendacionService.RecomendarAsync(usuarioId, LimitesPlan.MaxFaltantesRecomendacion)).ToList();
        candidatas = FiltrarPorSabor(candidatas, sabor);
        var completas = candidatas.Where(receta => receta.Grupo == GrupoRecomendacion.Completa).ToList();
        var recomendaciones = completas.Count > 0 ? completas : candidatas;

        if (recomendaciones.Count == 0)
        {
            var generada = await GenerarRecetaSinCoincidenciasAsync(usuarioId, mensajes, systemInstruction, sabor);
            if (generada is not null)
            {
                return generada;
            }
        }

        var mensajeSistema = recomendaciones.Count == 0
            ? "No encontré recetas de ese tipo que puedas hacer con lo que tenés. Cargá más ingredientes o pedime que te genere una."
            : sabor == SaborPedido.Dulce
                ? "Estas opciones son dulces, del mismo estilo que pediste:"
                : sabor == SaborPedido.Salado
                    ? "Estas opciones son saladas, del mismo estilo que pediste:"
                    : completas.Count > 0
                        ? "Con lo que tenés en tu alacena podés hacer estas recetas:"
                        : $"Te faltan pocos ingredientes (como máximo {LimitesPlan.MaxFaltantesRecomendacion}) para estas recetas:";

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.Recomendar,
            Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje) || recomendaciones.Count == 0
                ? mensajeSistema
                : $"{sobre.Mensaje} {mensajeSistema}",
            Recomendaciones = recomendaciones
        };
    }

    private async Task<ChatRespuestaDto?> GenerarRecetaSinCoincidenciasAsync(
        int usuarioId,
        List<MensajeIa> mensajes,
        string systemInstruction,
        SaborPedido sabor)
    {
        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId && item.Cantidad > 0);
        var alacenaVacia = stock.Count == 0;
        var pedido = sabor == SaborPedido.Dulce
            ? PedidoRecetaDulce
            : sabor == SaborPedido.Salado
                ? PedidoRecetaSalada
                : alacenaVacia ? PedidoRecetaAlacenaVacia : PedidoRecetaSinCoincidencias;
        var mensajeSistema = sabor == SaborPedido.Dulce
            ? "No encontré postres en el catálogo para eso, así que te armé uno dulce."
            : sabor == SaborPedido.Salado
                ? "No encontré platos salados en el catálogo para eso, así que te armé uno salado."
                : alacenaVacia ? MensajeRecetaAlacenaVacia : MensajeRecetaSinCoincidencias;

        var conversacion = new List<MensajeIa>(mensajes) { new("user", pedido) };

        string json;
        try
        {
            json = await _asistenteIa.GenerarRespuestaConversacionAsync(conversacion, systemInstruction, jsonMode: true);
        }
        catch (ServicioExternoException)
        {
            return null;
        }

        var sobre = DeserializarSobre(json);
        if (sobre?.Receta is null)
        {
            return null;
        }

        var respuesta = await ResolverGenerarRecetaAsync(usuarioId, sobre);
        if (respuesta.Accion != ChatAccion.GenerarReceta)
        {
            return null;
        }

        var presentacion = SanitizarMensajePresentacionReceta(sobre.Mensaje, string.Empty);
        respuesta.Mensaje = string.IsNullOrWhiteSpace(presentacion)
            ? mensajeSistema
            : $"{mensajeSistema} {presentacion}";
        return respuesta;
    }

    private async Task<List<RecetaFavorita>> ObtenerFavoritosAccesiblesAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        var favoritos = await _favoritoRepository.FindWithIncludesAsync(
            favorito => favorito.UsuarioId == usuarioId,
            favorito => favorito.Receta);

        return favoritos
            .Where(favorito => favorito.Receta is not null &&
                AccesoReceta.PuedeVer(favorito.Receta, usuarioId, usuario?.Rol))
            .ToList();
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

        var propias = (await _recetaService.GetAllAsync(usuarioId))
            .Where(receta => receta.UsuarioId == usuarioId)
            .ToList();
        var favoritos = await ObtenerFavoritosAccesiblesAsync(usuarioId);

        if (sobre.EliminarTodas)
        {
            var idsPropias = propias.Select(receta => receta.Id).ToHashSet();
            var favoritosCatalogo = favoritos.Where(favorito => !idsPropias.Contains(favorito.RecetaId)).ToList();
            var total = idsPropias.Count + favoritosCatalogo.Count;

            if (total == 0)
            {
                return new ChatRespuestaDto
                {
                    Accion = ChatAccion.EliminarReceta,
                    Mensaje = "No tenés recetas guardadas para borrar.",
                    RecetasEliminadas = 0
                };
            }

            await _unidadDeTrabajo.EjecutarAsync(async () =>
            {
                foreach (var favorito in favoritosCatalogo)
                {
                    await _favoritoRepository.DeleteAsync(favorito);
                }

                foreach (var recetaId in idsPropias)
                {
                    await _recetaService.DeleteAsync(recetaId, usuarioId);
                }
            });
            _cache.Remove(RecetaService.CacheCatalogoKey);

            return new ChatRespuestaDto
            {
                Accion = ChatAccion.EliminarReceta,
                Mensaje = $"Listo, borré tus {total} receta(s) guardada(s).",
                RecetasEliminadas = total
            };
        }

        var propiasCoincidentes = BusquedaDifusa.BuscarMejores(propias, receta => receta.Titulo, titulo);
        var idsPropiasCoincidentes = propiasCoincidentes.Select(receta => receta.Id).ToHashSet();
        var favoritosCoincidentes = BusquedaDifusa.BuscarMejores(
            favoritos.Where(favorito => favorito.Receta is not null && !idsPropiasCoincidentes.Contains(favorito.RecetaId)),
            favorito => favorito.Receta.Titulo,
            titulo);

        var totalCoincidencias = propiasCoincidentes.Count + favoritosCoincidentes.Count;
        if (totalCoincidencias > 1)
        {
            var nombres = propiasCoincidentes.Select(receta => receta.Titulo)
                .Concat(favoritosCoincidentes.Select(favorito => favorito.Receta.Titulo))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(nombre => $"\"{nombre}\"");

            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = $"Encontré varias recetas parecidas a \"{titulo}\": {string.Join(", ", nombres)}. ¿Cuál querés borrar?"
            };
        }

        if (totalCoincidencias == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.EliminarReceta,
                Mensaje = $"No encontré ninguna receta guardada que coincida con \"{titulo}\".",
                RecetasEliminadas = 0
            };
        }

        string tituloBorrado;
        if (propiasCoincidentes.Count == 1)
        {
            var receta = propiasCoincidentes[0];
            tituloBorrado = receta.Titulo;
            await _recetaService.DeleteAsync(receta.Id, usuarioId);
        }
        else
        {
            var favorito = favoritosCoincidentes[0];
            tituloBorrado = favorito.Receta.Titulo;
            await _favoritoRepository.DeleteAsync(favorito);
        }

        _cache.Remove(RecetaService.CacheCatalogoKey);
        return new ChatRespuestaDto
        {
            Accion = ChatAccion.EliminarReceta,
            Mensaje = $"Listo, borré \"{tituloBorrado}\".",
            RecetasEliminadas = 1
        };
    }

    private async Task<ChatRespuestaDto> ResolverPlanificarRecetaAsync(
        int usuarioId,
        SobreAgente sobre,
        IReadOnlyList<ChatMessageDto> turnos)
    {
        var hoy = FechaLocal.Hoy();
        var turnosFlujo = ObtenerTurnosFlujoPlanificacion(turnos);
        var ultimoTexto = turnos.Count > 0 ? turnos[^1].Texto : string.Empty;

        var tituloEnPreguntaPrevia = ExtraerTituloDePreguntaPlanificacion(turnosFlujo);
        var tituloPedido = !string.IsNullOrWhiteSpace(sobre.TituloPlanificar)
            ? sobre.TituloPlanificar.Trim()
            : tituloEnPreguntaPrevia;

        if (!string.IsNullOrWhiteSpace(tituloEnPreguntaPrevia) &&
            !string.IsNullOrWhiteSpace(tituloPedido) &&
            !BusquedaDifusa.Coincide(tituloPedido, tituloEnPreguntaPrevia) &&
            !UsuarioMencionoOtraReceta(ultimoTexto, tituloPedido))
        {
            tituloPedido = tituloEnPreguntaPrevia;
        }

        var indiceEfectivo = string.IsNullOrWhiteSpace(tituloPedido) ? sobre.IndiceReceta : null;
        var (recetaId, tituloReceta, errorReceta) = await ResolverRecetaParaAccionAsync(
            usuarioId,
            tituloPedido,
            indiceEfectivo,
            "planificarla");
        if (recetaId is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = errorReceta!
            };
        }

        var turno = ResolverTurnoEnFlujo(turnosFlujo);
        var fecha = ResolverFechaEnFlujo(turnosFlujo, hoy) ?? ParsearFechaOpcional(sobre.FechaPlanificar);
        if (turno is null || fecha is null)
        {
            var faltaTurno = turno is null;
            var faltaFecha = fecha is null;
            var pregunta = faltaTurno && faltaFecha
                ? $"¿Qué día y qué comida querés para \"{tituloReceta}\"? Por ejemplo: el día 25 o el domingo, al almuerzo."
                : faltaTurno
                    ? $"¿Para qué comida querés planificar \"{tituloReceta}\" el {fecha:dd/MM}? Elegí Desayuno, Almuerzo, Merienda o Cena."
                    : $"¿Qué día querés planificar \"{tituloReceta}\" ({turno})? Podés decir hoy, mañana, el día 25 o el domingo que viene.";
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = pregunta
            };
        }

        var reemplazar = sobre.ReemplazarSiConflicto || EsConfirmacionReemplazo(turnosFlujo, ultimoTexto);

        try
        {
            var resultado = await _planificadorService.PlanificarAsync(new ComidaPlanificadaCreateDto
            {
                UsuarioId = usuarioId,
                RecetaId = recetaId.Value,
                FechaAsignada = fecha.Value,
                Turno = turno,
                ReemplazarSiConflicto = reemplazar
            });

            var agregados = resultado.IngredientesAgregadosALista;
            var aviso = agregados.Count > 0
                ? $" Te faltaban algunos ingredientes, así que los agregué a tu lista de compras: {string.Join(", ", agregados)}."
                : " Ya tenías todos los ingredientes en tu alacena.";

            return new ChatRespuestaDto
            {
                Accion = ChatAccion.PlanificarReceta,
                Mensaje = $"Planifiqué \"{tituloReceta}\" para el {fecha:dd/MM/yyyy} ({turno})." + aviso,
                ComidaPlanificada = resultado.Comida,
                IngredientesAgregadosALista = agregados
            };
        }
        catch (ConflictException ex)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = $"{ex.Message} ¿Querés que reemplace esa comida por \"{tituloReceta}\"? Decime \"sí, reemplazala\".",
                RequiereConfirmacionReemplazo = true
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or ForbiddenException or KeyNotFoundException or ArgumentException)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = ex.Message
            };
        }
    }

    private async Task<ChatRespuestaDto> ResolverEliminarComidaPlanificadaAsync(
        int usuarioId,
        SobreAgente sobre,
        IReadOnlyList<ChatMessageDto> turnos)
    {
        var hoy = FechaLocal.Hoy();
        var ultimoTexto = turnos.Count > 0 ? turnos[^1].Texto : string.Empty;
        var comidas = (await _planificadorService.GetByUsuarioIdAsync(usuarioId))
            .Where(comida => comida.FechaAsignada.Date >= hoy)
            .OrderBy(comida => comida.FechaAsignada)
            .ToList();

        if (comidas.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.EliminarComidaPlanificada,
                Mensaje = "No tenés comidas planificadas próximas para quitar."
            };
        }

        if (sobre.EliminarTodas)
        {
            foreach (var comida in comidas)
            {
                await _planificadorService.DeleteAsync(comida.Id, usuarioId);
            }

            return new ChatRespuestaDto
            {
                Accion = ChatAccion.EliminarComidaPlanificada,
                Mensaje = $"Listo, limpié tus {comidas.Count} comida(s) del planificador."
            };
        }

        var fecha = FechaExplicita(ultimoTexto, hoy) ?? ParsearFechaOpcional(sobre.FechaPlanificar);
        var turno = TurnoExplicito(ultimoTexto) ?? NormalizarTurnoOpcional(sobre.TurnoPlanificar);
        var titulo = string.IsNullOrWhiteSpace(sobre.TituloPlanificar)
            ? sobre.TituloEliminar?.Trim()
            : sobre.TituloPlanificar.Trim();

        IEnumerable<ComidaPlanificadaResponseDto> filtradas = comidas;
        if (fecha is not null)
        {
            filtradas = filtradas.Where(comida => comida.FechaAsignada.Date == fecha.Value.Date);
        }

        if (turno is not null)
        {
            filtradas = filtradas.Where(comida => string.Equals(comida.Turno, turno, StringComparison.OrdinalIgnoreCase));
        }

        var lista = filtradas.ToList();
        if (!string.IsNullOrWhiteSpace(titulo))
        {
            var porTitulo = BusquedaDifusa.BuscarMejores(lista, comida => comida.RecetaTitulo, titulo);
            if (porTitulo.Count > 0 || (fecha is null && turno is null))
            {
                lista = porTitulo;
            }
        }

        if (lista.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No encontré esa comida en tu planificador. Decime el día, el turno o el nombre de la receta que querés quitar."
            };
        }

        if (lista.Count > 1 && fecha is null && turno is null)
        {
            var opciones = lista.Select(comida => $"\"{comida.RecetaTitulo}\" ({comida.FechaAsignada:dd/MM}, {comida.Turno})");
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = $"Encontré varias comidas planificadas que coinciden: {string.Join(", ", opciones)}. ¿Cuál querés quitar?"
            };
        }

        foreach (var comida in lista)
        {
            await _planificadorService.DeleteAsync(comida.Id, usuarioId);
        }

        var resumen = string.Join(", ", lista.Select(comida => $"\"{comida.RecetaTitulo}\" ({comida.FechaAsignada:dd/MM}, {comida.Turno})"));
        return new ChatRespuestaDto
        {
            Accion = ChatAccion.EliminarComidaPlanificada,
            Mensaje = $"Listo, quité del planificador: {resumen}."
        };
    }

    private async Task<ChatRespuestaDto> ResolverAgregarIngredienteAlacenaAsync(int usuarioId, SobreAgente sobre)
    {
        var items = (sobre.IngredientesAlacena ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Nombre))
            .ToList();

        if (items.Count == 0 && !string.IsNullOrWhiteSpace(sobre.Nombre))
        {
            items.Add(new IngredienteAlacenaChatDto { Nombre = sobre.Nombre.Trim(), Cantidad = 1, UnidadMedida = UnidadesMedida.Unidad });
        }

        if (items.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "¿Qué ingrediente y qué cantidad querés agregar a tu alacena?"
            };
        }

        var agregados = new List<string>();
        var errores = new List<string>();

        foreach (var item in items)
        {
            var cantidad = item.Cantidad is > 0 ? item.Cantidad.Value : 1m;
            var unidad = string.IsNullOrWhiteSpace(item.UnidadMedida) ? UnidadesMedida.Unidad : item.UnidadMedida.Trim();
            try
            {
                var creado = await _stockUsuarioService.CreateAsync(new StockUsuarioCreateDto
                {
                    UsuarioId = usuarioId,
                    NombreIngrediente = item.Nombre!.Trim(),
                    Cantidad = cantidad,
                    UnidadMedida = unidad
                });
                agregados.Add($"{creado.NombreIngrediente} ({cantidad} {unidad})");
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                errores.Add($"{item.Nombre!.Trim()}: {ex.Message}");
            }
        }

        if (agregados.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = string.Join(" ", errores)
            };
        }

        var mensaje = $"Listo, sumé a tu alacena: {string.Join(", ", agregados)}.";
        if (errores.Count > 0)
        {
            mensaje += $" No pude agregar: {string.Join(" | ", errores)}.";
        }

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.AgregarIngredienteAlacena,
            Mensaje = mensaje
        };
    }

    private async Task<ChatRespuestaDto> ResolverActualizarIngredienteAlacenaAsync(int usuarioId, SobreAgente sobre)
    {
        var items = (sobre.IngredientesAlacena ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Nombre))
            .ToList();

        if (items.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "¿Qué ingrediente de tu alacena querés actualizar y a qué cantidad?"
            };
        }

        var stock = (await _stockRepository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId,
            item => item.Ingrediente)).ToList();

        var actualizados = new List<string>();
        var eliminados = new List<string>();
        var noEncontrados = new List<string>();

        foreach (var pedido in items)
        {
            var coincidencias = BusquedaDifusa.BuscarMejores(
                stock,
                item => item.Ingrediente?.Nombre ?? string.Empty,
                pedido.Nombre);

            if (coincidencias.Count == 0)
            {
                noEncontrados.Add(pedido.Nombre!.Trim());
                continue;
            }

            if (coincidencias.Count > 1)
            {
                var nombres = coincidencias.Select(item => $"\"{item.Ingrediente?.Nombre}\"");
                return new ChatRespuestaDto
                {
                    Accion = ChatAccion.Conversar,
                    Mensaje = $"Tenés varios ingredientes parecidos a \"{pedido.Nombre}\": {string.Join(", ", nombres)}. ¿Cuál querés actualizar?"
                };
            }

            var objetivo = coincidencias[0];
            var nombreReal = objetivo.Ingrediente?.Nombre ?? pedido.Nombre!.Trim();
            var nuevaCantidad = pedido.Cantidad ?? objetivo.Cantidad;
            if (nuevaCantidad <= 0)
            {
                await _stockUsuarioService.DeleteAsync(objetivo.Id, usuarioId);
                eliminados.Add(nombreReal);
                continue;
            }

            var nuevaUnidad = string.IsNullOrWhiteSpace(pedido.UnidadMedida)
                ? objetivo.UnidadMedida
                : pedido.UnidadMedida.Trim();

            await _stockUsuarioService.UpdateAsync(objetivo.Id, new StockUsuarioUpdateDto
            {
                Cantidad = nuevaCantidad,
                UnidadMedida = nuevaUnidad,
                FechaCaducidad = objetivo.FechaCaducidad
            }, usuarioId);
            actualizados.Add($"{nombreReal} ({nuevaCantidad} {nuevaUnidad})");
        }

        var partes = new List<string>();
        if (actualizados.Count > 0)
        {
            partes.Add($"Actualicé en tu alacena: {string.Join(", ", actualizados)}.");
        }

        if (eliminados.Count > 0)
        {
            partes.Add($"Quité de tu alacena: {string.Join(", ", eliminados)}.");
        }

        if (noEncontrados.Count > 0)
        {
            partes.Add($"No encontré en tu alacena: {string.Join(", ", noEncontrados)}.");
        }

        return new ChatRespuestaDto
        {
            Accion = actualizados.Count > 0 || eliminados.Count > 0
                ? ChatAccion.ActualizarIngredienteAlacena
                : ChatAccion.Conversar,
            Mensaje = string.Join(" ", partes)
        };
    }

    private async Task<ChatRespuestaDto> ResolverEliminarIngredienteAlacenaAsync(
        int usuarioId,
        SobreAgente sobre,
        string ultimoTexto)
    {
        var stock = (await _stockRepository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId,
            item => item.Ingrediente)).ToList();

        if (stock.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.EliminarIngredienteAlacena,
                Mensaje = "Tu alacena ya está vacía."
            };
        }

        if (sobre.EliminarTodosAlacena || sobre.EliminarTodas)
        {
            foreach (var item in stock)
            {
                await _stockUsuarioService.DeleteAsync(item.Id, usuarioId);
            }

            return new ChatRespuestaDto
            {
                Accion = ChatAccion.EliminarIngredienteAlacena,
                Mensaje = $"Listo, vacié tu alacena (eliminé {stock.Count} ingrediente(s))."
            };
        }

        var nombresPedidos = (sobre.IngredientesAlacena ?? [])
            .Select(item => item.Nombre?.Trim())
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Select(nombre => nombre!)
            .ToList();

        if (nombresPedidos.Count == 0 && !string.IsNullOrWhiteSpace(sobre.TituloEliminar))
        {
            nombresPedidos.Add(sobre.TituloEliminar.Trim());
        }

        if (nombresPedidos.Count == 0 && !string.IsNullOrWhiteSpace(sobre.Nombre))
        {
            nombresPedidos.Add(sobre.Nombre.Trim());
        }

        if (nombresPedidos.Count == 0)
        {
            var inferidos = InferirIngredientesDesdeTexto(stock, ultimoTexto);
            nombresPedidos.AddRange(inferidos);
        }

        if (nombresPedidos.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "¿Qué ingrediente querés eliminar de tu alacena?"
            };
        }

        var aEliminar = new List<StockUsuario>();
        var idsAgregados = new HashSet<int>();
        var noEncontrados = new List<string>();

        foreach (var pedido in nombresPedidos)
        {
            var coincidencias = BusquedaDifusa.BuscarMejores(
                stock,
                item => item.Ingrediente?.Nombre ?? string.Empty,
                pedido);

            if (coincidencias.Count == 0)
            {
                noEncontrados.Add(pedido);
                continue;
            }

            if (coincidencias.Count > 1)
            {
                var opciones = coincidencias.Select(item => $"\"{item.Ingrediente?.Nombre}\"");
                return new ChatRespuestaDto
                {
                    Accion = ChatAccion.Conversar,
                    Mensaje = $"Encontré varios ingredientes parecidos a \"{pedido}\" en tu alacena: {string.Join(", ", opciones)}. ¿Cuál querés borrar?"
                };
            }

            if (idsAgregados.Add(coincidencias[0].Id))
            {
                aEliminar.Add(coincidencias[0]);
            }
        }

        if (aEliminar.Count == 0)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = $"No encontré {string.Join(", ", noEncontrados.Select(item => $"\"{item}\""))} en tu alacena."
            };
        }

        var borrados = new List<string>();
        foreach (var item in aEliminar)
        {
            await _stockUsuarioService.DeleteAsync(item.Id, usuarioId);
            borrados.Add(item.Ingrediente?.Nombre ?? $"#{item.IngredienteId}");
        }

        var mensaje = $"Listo, eliminé de tu alacena: {string.Join(", ", borrados)}.";
        if (noEncontrados.Count > 0)
        {
            mensaje += $" No encontré: {string.Join(", ", noEncontrados)}.";
        }

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.EliminarIngredienteAlacena,
            Mensaje = mensaje
        };
    }

    private static List<string> InferirIngredientesDesdeTexto(List<StockUsuario> stock, string texto)
    {
        var palabras = BusquedaDifusa.Normalizar(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(palabra => palabra.Length >= 3)
            .ToList();

        var encontrados = new List<string>();
        foreach (var item in stock)
        {
            var nombre = item.Ingrediente?.Nombre;
            if (string.IsNullOrWhiteSpace(nombre))
            {
                continue;
            }

            if (palabras.Any(palabra => BusquedaDifusa.Coincide(nombre, palabra)))
            {
                encontrados.Add(nombre);
            }
        }

        return encontrados;
    }

    private async Task<ChatRespuestaDto> ResolverConsultarRecetasGuardadasAsync(int usuarioId, SobreAgente sobre)
    {
        var favoritos = (await _favoritoService.GetByUsuarioIdAsync(usuarioId)).ToList();
        var cantidad = favoritos.Count;

        var mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
            ? (cantidad == 0
                ? "No tenés recetas guardadas por ahora."
                : $"Tenés {cantidad} receta(s) guardada(s) en favoritos.")
            : sobre.Mensaje;

        return new ChatRespuestaDto
        {
            Accion = ChatAccion.ConsultarRecetasGuardadas,
            Mensaje = mensaje,
            CantidadRecetasGuardadas = cantidad
        };
    }

    private async Task<(int? recetaId, string? titulo, string? error)> ResolverRecetaParaAccionAsync(
        int usuarioId,
        string? tituloPedido,
        int? indiceReceta,
        string proposito)
    {
        var titulo = tituloPedido?.Trim();
        var recientes = await ObtenerRecetasRecientesAsync(usuarioId);
        if (indiceReceta is < 1 || indiceReceta > recientes.Count)
        {
            indiceReceta = null;
        }

        if (!string.IsNullOrWhiteSpace(titulo))
        {
            var accesibles = (await _recetaService.GetAllAsync(usuarioId)).ToList();
            var propias = accesibles.Where(receta => receta.UsuarioId == usuarioId).ToList();
            var propia = BusquedaDifusa.BuscarMejor(propias, receta => receta.Titulo, titulo);
            if (propia is not null)
            {
                return (propia.Id, propia.Titulo, null);
            }

            var favoritos = await ObtenerFavoritosAccesiblesAsync(usuarioId);
            var favorito = BusquedaDifusa.BuscarMejor(
                favoritos.Where(item => item.Receta is not null).Select(item => item.Receta),
                receta => receta.Titulo,
                titulo);
            if (favorito is not null)
            {
                return (favorito.Id, favorito.Titulo, null);
            }

            var catalogo = accesibles.Where(receta => receta.UsuarioId is null).ToList();
            var delCatalogo = BusquedaDifusa.BuscarMejor(catalogo, receta => receta.Titulo, titulo);
            if (delCatalogo is not null)
            {
                return (delCatalogo.Id, delCatalogo.Titulo, null);
            }

            var recientePorTitulo = BusquedaDifusa.BuscarMejor(recientes, receta => receta.Titulo, titulo);
            if (recientePorTitulo is not null)
            {
                try
                {
                    var guardada = await _recetaIaService.GuardarRecetaAsync(usuarioId, recientePorTitulo);
                    await OlvidarRecetaAsync(usuarioId, recientePorTitulo.Titulo);
                    return (guardada.RecetaId, guardada.Titulo, null);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ForbiddenException or ConflictException or ArgumentException)
                {
                    return (null, null, $"No pude preparar la receta para {proposito}: {ex.Message}");
                }
            }

            return (null, null, $"No encontré \"{titulo}\" entre tus recetas guardadas ni entre las últimas que te propuse.");
        }

        var recordada = BuscarEnRecientes(recientes, indiceReceta, null) ?? recientes.FirstOrDefault();
        if (recordada is null)
        {
            return (null, null,
                "No sé a qué receta te referís. Generá o guardá una receta primero, o decime el nombre de una que ya tengas guardada.");
        }

        try
        {
            var guardada = await _recetaIaService.GuardarRecetaAsync(usuarioId, recordada);
            await OlvidarRecetaAsync(usuarioId, recordada.Titulo);
            return (guardada.RecetaId, guardada.Titulo, null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ForbiddenException or ConflictException or ArgumentException)
        {
            return (null, null, $"No pude preparar la receta para {proposito}: {ex.Message}");
        }
    }

    private async Task<ChatRespuestaDto> ResolverCocinarRecetaAsync(int usuarioId, SobreAgente sobre)
    {
        var (recetaId, tituloReceta, error) = await ResolverRecetaParaAccionAsync(
            usuarioId,
            sobre.TituloCocinar,
            sobre.IndiceReceta,
            "descontar el stock");
        if (recetaId is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = error!
            };
        }

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
                : " No tenías stock cargado de los ingredientes, así que no desconté nada.";
            if (faltante.Count > 0)
            {
                aviso += $" Ojo, no te alcanzaba para: {string.Join(", ", faltante)}.";
            }

            if (resultado.NoDescontados.Count > 0)
            {
                aviso += $" No toqué {string.Join(", ", resultado.NoDescontados.Select(item => item.Nombre))} porque en tu alacena está cargado en otra unidad; ajustalo a mano.";
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
        catch (Exception ex) when (ex is InvalidOperationException or ForbiddenException or KeyNotFoundException or ArgumentException)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = $"No pude descontar el stock: {ex.Message}"
            };
        }
    }

    private static readonly string[] BasesSaladasEnPostre =
    [
        "lenteja", "garbanzo", "poroto", "polenta", "arroz", "fideo", "carne", "pollo", "papa"
    ];

    private static bool TieneBaseSalada(string? texto)
    {
        var limpio = (texto ?? string.Empty).ToLowerInvariant();
        return BasesSaladasEnPostre.Any(baseSalada => limpio.Contains(baseSalada, StringComparison.Ordinal));
    }

    private static bool RecetaEsPostreFalso(RecetaGeneradaDto receta) =>
        TieneBaseSalada(receta.Titulo) ||
        receta.Ingredientes.Any(ingrediente => TieneBaseSalada(ingrediente.Nombre));

    private async Task<SobreAgente> EvitarPostreFalsoAsync(
        SobreAgente sobre,
        List<MensajeIa> mensajes,
        string systemInstruction,
        string pedidoUsuario)
    {
        if (DetectarSabor(pedidoUsuario) != SaborPedido.Dulce ||
            sobre.Receta is null ||
            !RecetaEsPostreFalso(sobre.Receta))
        {
            return sobre;
        }

        var conversacion = new List<MensajeIa>(mensajes)
        {
            new("user",
                $"\"{sobre.Receta.Titulo}\" no es un postre: usa una base salada de la alacena. " +
                "Generá un postre real sin azúcar (fruta, huevo o avena). " +
                "Prohibido lentejas, garbanzos, arroz, polenta, papa, fideos o carne. " +
                "Respondé con accion 'generar_receta'.")
        };

        try
        {
            var json = await _asistenteIa.GenerarRespuestaConversacionAsync(conversacion, systemInstruction, jsonMode: true);
            var alternativa = DeserializarSobre(json);
            if (alternativa?.Receta is { Ingredientes.Count: > 0 } &&
                string.Equals(alternativa.Accion?.Trim(), ChatAccion.GenerarReceta, StringComparison.OrdinalIgnoreCase) &&
                !RecetaEsPostreFalso(alternativa.Receta))
            {
                return alternativa;
            }
        }
        catch (ServicioExternoException)
        {
        }

        sobre.Accion = ChatAccion.Conversar;
        sobre.Receta = null;
        sobre.Mensaje = "Eso no es un postre: no voy a usar lentejas, arroz o polenta para hacerlo dulce. " +
            "Pedime un postre sin azúcar, por ejemplo manzanas al horno o flan sin azúcar.";
        return sobre;
    }

    private static List<ChatMessageDto> ObtenerTurnosFlujoPlanificacion(IReadOnlyList<ChatMessageDto> turnos)
    {
        var resultado = new List<ChatMessageDto>();
        for (var i = turnos.Count - 1; i >= 0 && resultado.Count < 8; i--)
        {
            var turno = turnos[i];
            var esAsistente = NormalizarRolGroq(turno.Rol) == "assistant";
            if (esAsistente && EsCorteDeFlujoPlanificacion(turno.Texto))
            {
                break;
            }

            resultado.Add(turno);
        }

        resultado.Reverse();
        return resultado;
    }

    private static bool EsCorteDeFlujoPlanificacion(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = texto.Trim();
        return limpio.StartsWith("Planifiqué ", StringComparison.OrdinalIgnoreCase) ||
               limpio.StartsWith("Te propongo:", StringComparison.OrdinalIgnoreCase) ||
               limpio.StartsWith("Listo, ", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtraerTituloDePreguntaPlanificacion(IReadOnlyList<ChatMessageDto> turnosFlujo)
    {
        for (var i = turnosFlujo.Count - 1; i >= 0; i--)
        {
            var turno = turnosFlujo[i];
            if (NormalizarRolGroq(turno.Rol) != "assistant")
            {
                continue;
            }

            var texto = turno.Texto;
            var esPreguntaPlan =
                texto.Contains("planificar", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("reemplace esa comida", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("qué día y qué comida", StringComparison.OrdinalIgnoreCase);

            if (!esPreguntaPlan)
            {
                continue;
            }

            var inicio = texto.IndexOf('"');
            if (inicio < 0)
            {
                continue;
            }

            var fin = texto.IndexOf('"', inicio + 1);
            if (fin > inicio + 1)
            {
                var candidato = texto[(inicio + 1)..fin].Trim();
                if (!candidato.Equals("sí, reemplazala", StringComparison.OrdinalIgnoreCase))
                {
                    return candidato;
                }
            }
        }

        return null;
    }

    private static bool UsuarioMencionoOtraReceta(string ultimoTexto, string tituloPropuesto)
    {
        var palabrasTexto = BusquedaDifusa.Normalizar(ultimoTexto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(palabra => palabra.Length >= 4)
            .ToList();
        if (palabrasTexto.Count == 0)
        {
            return false;
        }

        var palabrasTitulo = BusquedaDifusa.Normalizar(tituloPropuesto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(palabra => palabra.Length >= 4)
            .ToList();

        return palabrasTitulo.Any(palabraTitulo =>
            palabrasTexto.Any(palabraTexto => BusquedaDifusa.Coincide(palabraTitulo, palabraTexto)));
    }

    private static string? ResolverTurnoEnFlujo(IReadOnlyList<ChatMessageDto> turnosFlujo)
    {
        for (var i = turnosFlujo.Count - 1; i >= 0; i--)
        {
            if (NormalizarRolGroq(turnosFlujo[i].Rol) != "user")
            {
                continue;
            }

            var encontrado = TurnoExplicito(turnosFlujo[i].Texto);
            if (encontrado is not null)
            {
                return encontrado;
            }
        }

        return null;
    }

    private static DateTime? ResolverFechaEnFlujo(
        IReadOnlyList<ChatMessageDto> turnosFlujo,
        DateTime hoy)
    {
        for (var i = turnosFlujo.Count - 1; i >= 0; i--)
        {
            if (NormalizarRolGroq(turnosFlujo[i].Rol) != "user")
            {
                continue;
            }

            var encontrada = FechaExplicita(turnosFlujo[i].Texto, hoy, permitirNumeroSuelto: true);
            if (encontrada is not null)
            {
                return encontrada;
            }
        }

        for (var i = turnosFlujo.Count - 1; i >= 0; i--)
        {
            if (NormalizarRolGroq(turnosFlujo[i].Rol) != "assistant")
            {
                continue;
            }

            var enPregunta = FechaNumericaExplicita(turnosFlujo[i].Texto, hoy);
            if (enPregunta is not null)
            {
                return enPregunta;
            }
        }

        return null;
    }

    private static bool EsConfirmacionReemplazo(IReadOnlyList<ChatMessageDto> turnosFlujo, string ultimoTexto)
    {
        var huboConflicto = turnosFlujo.Any(turno =>
            NormalizarRolGroq(turno.Rol) == "assistant" &&
            turno.Texto.Contains("reemplac", StringComparison.OrdinalIgnoreCase));
        if (!huboConflicto)
        {
            return false;
        }

        var limpio = QuitarTildes(ultimoTexto.Trim().ToLowerInvariant());
        return limpio.Contains("reemplaz", StringComparison.Ordinal) ||
               limpio.Contains("cambial", StringComparison.Ordinal) ||
               limpio == "si" ||
               limpio.StartsWith("si,", StringComparison.Ordinal) ||
               limpio.StartsWith("si ", StringComparison.Ordinal) ||
               limpio.Contains("dale", StringComparison.Ordinal);
    }

    private static string? NormalizarTurnoOpcional(string? turno)
    {
        if (string.IsNullOrWhiteSpace(turno))
        {
            return null;
        }

        return TurnosComida.Validos.FirstOrDefault(opcion =>
            opcion.Equals(turno.Trim(), StringComparison.OrdinalIgnoreCase)) ?? TurnoExplicito(turno);
    }

    private static DateTime? ParsearFechaOpcional(string? fecha)
    {
        if (!string.IsNullOrWhiteSpace(fecha) &&
            DateTime.TryParseExact(fecha.Trim(), FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parseada))
        {
            return parseada.Date;
        }

        return null;
    }

    private static readonly (string Nombre, int Mes)[] MesesAnio =
    [
        ("enero", 1), ("febrero", 2), ("marzo", 3), ("abril", 4),
        ("mayo", 5), ("junio", 6), ("julio", 7), ("agosto", 8),
        ("septiembre", 9), ("setiembre", 9), ("octubre", 10), ("noviembre", 11), ("diciembre", 12)
    ];

    private static DateTime? FechaExplicita(string? texto, DateTime hoy, bool permitirNumeroSuelto = false)
    {
        var limpio = QuitarTildes((texto ?? string.Empty).ToLowerInvariant());
        if (limpio.Contains("pasado manana", StringComparison.Ordinal))
        {
            return hoy.AddDays(2);
        }

        if (limpio.Contains("manana", StringComparison.Ordinal))
        {
            return hoy.AddDays(1);
        }

        if (limpio.Contains("hoy", StringComparison.Ordinal))
        {
            return hoy;
        }

        var numerica = FechaNumericaExplicita(limpio, hoy);
        if (numerica is not null)
        {
            return numerica;
        }

        foreach (var (nombreMes, mes) in MesesAnio)
        {
            var matchMes = Regex.Match(limpio, $@"\b(\d{{1,2}})\s+de\s+{nombreMes}\b", RegexOptions.CultureInvariant);
            if (!matchMes.Success)
            {
                continue;
            }

            if (int.TryParse(matchMes.Groups[1].Value, out var diaMes) && diaMes is >= 1 and <= 31)
            {
                var anio = hoy.Year;
                if (diaMes <= DateTime.DaysInMonth(anio, mes))
                {
                    var fecha = new DateTime(anio, mes, diaMes);
                    if (fecha.Date < hoy.Date)
                    {
                        fecha = fecha.AddYears(1);
                    }

                    return fecha;
                }
            }
        }

        var diaConPalabra = Regex.Match(
            limpio,
            @"\b(?:el\s+)?(?:proximo\s+|prox\.?\s+)?dia\s+(\d{1,2})\b",
            RegexOptions.CultureInvariant);
        if (diaConPalabra.Success && int.TryParse(diaConPalabra.Groups[1].Value, out var diaPalabra))
        {
            var resuelta = ResolverDiaDelMes(diaPalabra, hoy);
            if (resuelta is not null)
            {
                return resuelta;
            }
        }

        var diaConArticulo = Regex.Match(
            limpio,
            @"\b(?:para\s+el|el|al)\s+(?:proximo\s+|prox\.?\s+)?(\d{1,2})\b(?!\s*(?:de\s+[a-z]+|porcion|minuto|min\b|hora|hs\b|gr\b|g\b|kg\b|ml\b|l\b|taza|cucharad|unidad|receta))",
            RegexOptions.CultureInvariant);
        if (diaConArticulo.Success && int.TryParse(diaConArticulo.Groups[1].Value, out var diaArticulo))
        {
            var resuelta = ResolverDiaDelMes(diaArticulo, hoy);
            if (resuelta is not null)
            {
                return resuelta;
            }
        }

        (string Nombre, DayOfWeek Dia)[] dias =
        [
            ("domingo", DayOfWeek.Sunday),
            ("lunes", DayOfWeek.Monday),
            ("martes", DayOfWeek.Tuesday),
            ("miercoles", DayOfWeek.Wednesday),
            ("jueves", DayOfWeek.Thursday),
            ("viernes", DayOfWeek.Friday),
            ("sabado", DayOfWeek.Saturday)
        ];

        foreach (var (nombre, dia) in dias)
        {
            if (!limpio.Contains(nombre, StringComparison.Ordinal))
            {
                continue;
            }

            var delta = ((int)dia - (int)hoy.DayOfWeek + 7) % 7;
            if (delta == 0)
            {
                delta = 7;
            }

            return hoy.AddDays(delta);
        }

        if (permitirNumeroSuelto)
        {
            var matchSuelto = Regex.Match(
                limpio,
                @"(?<!\b(?:receta|opcion|numero|nro|paso)\s+)\b(\d{1,2})\b(?!\s*(?:porcion|minuto|min\b|hora|hs\b|gr\b|g\b|kg\b|ml\b|l\b|taza|cucharad|unidad))",
                RegexOptions.CultureInvariant);
            if (matchSuelto.Success && int.TryParse(matchSuelto.Groups[1].Value, out var diaSuelto))
            {
                var resuelta = ResolverDiaDelMes(diaSuelto, hoy);
                if (resuelta is not null)
                {
                    return resuelta;
                }
            }
        }

        return null;
    }

    private static DateTime? FechaNumericaExplicita(string? texto, DateTime hoy)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var matchIso = Regex.Match(texto, @"\b(\d{4}-\d{2}-\d{2})\b", RegexOptions.CultureInvariant);
        if (matchIso.Success &&
            DateTime.TryParseExact(matchIso.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
        {
            return iso.Date;
        }

        var matchSlash = Regex.Match(texto, @"\b(\d{1,2}[/-]\d{1,2}(?:[/-]\d{4})?)\b", RegexOptions.CultureInvariant);
        if (!matchSlash.Success)
        {
            return null;
        }

        var normalizada = matchSlash.Groups[1].Value.Replace('-', '/');
        if (!DateTime.TryParseExact(
                normalizada,
                ["d/M", "dd/MM", "d/M/yyyy", "dd/MM/yyyy"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var numerica))
        {
            return null;
        }

        var tieneAnio = normalizada.Count(caracter => caracter == '/') == 2;
        var anio = tieneAnio ? numerica.Year : hoy.Year;
        if (numerica.Day > DateTime.DaysInMonth(anio, numerica.Month))
        {
            return null;
        }

        var fecha = new DateTime(anio, numerica.Month, numerica.Day);
        if (!tieneAnio && fecha.Date < hoy.Date)
        {
            fecha = fecha.AddYears(1);
        }

        return fecha;
    }

    private static DateTime? ResolverDiaDelMes(int dia, DateTime hoy)
    {
        if (dia is < 1 or > 31)
        {
            return null;
        }

        var inicio = new DateTime(hoy.Year, hoy.Month, 1);
        for (var offset = 0; offset <= 12; offset++)
        {
            var mes = inicio.AddMonths(offset);
            if (dia > DateTime.DaysInMonth(mes.Year, mes.Month))
            {
                continue;
            }

            var candidata = new DateTime(mes.Year, mes.Month, dia);
            if (candidata.Date >= hoy.Date)
            {
                return candidata;
            }
        }

        return null;
    }

    private static string QuitarTildes(string texto)
    {
        var formD = texto.Normalize(System.Text.NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var caracter in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(caracter);
            }
        }

        return builder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static string? TurnoExplicito(string? texto)
    {
        var limpio = QuitarTildes((texto ?? string.Empty).Trim().ToLowerInvariant());
        if (limpio.Contains("desayun", StringComparison.Ordinal))
        {
            return TurnosComida.Desayuno;
        }

        if (limpio.Contains("almuerz", StringComparison.Ordinal) ||
            limpio.Contains("almorz", StringComparison.Ordinal) ||
            limpio.Contains("mediodia", StringComparison.Ordinal))
        {
            return TurnosComida.Almuerzo;
        }

        if (limpio.Contains("meriend", StringComparison.Ordinal) ||
            limpio.Contains("merend", StringComparison.Ordinal) ||
            limpio.Contains("a la tarde", StringComparison.Ordinal) ||
            limpio.Contains("de tarde", StringComparison.Ordinal))
        {
            return TurnosComida.Merienda;
        }

        if (limpio.Contains("cena", StringComparison.Ordinal) ||
            limpio.Contains("a la noche", StringComparison.Ordinal) ||
            limpio.Contains("de noche", StringComparison.Ordinal) ||
            limpio.Contains("esta noche", StringComparison.Ordinal))
        {
            return TurnosComida.Cena;
        }

        return null;
    }

    private enum SaborPedido
    {
        Ninguno,
        Dulce,
        Salado
    }

    private static readonly string[] PalabrasDulces =
    [
        "dulce", "torta", "postre", "budin", "budín", "brownie", "flan", "bizcoch", "galleta",
        "galletita", "helado", "mousse", "pastel", "chocolate", "merengue"
    ];

    private static SaborPedido DetectarSabor(string? texto)
    {
        var limpio = (texto ?? string.Empty).ToLowerInvariant();
        var dulce = PalabrasDulces.Any(palabra => limpio.Contains(palabra, StringComparison.Ordinal));
        var salado = limpio.Contains("salado", StringComparison.Ordinal) ||
            limpio.Contains("salada", StringComparison.Ordinal);
        if (dulce && !salado)
        {
            return SaborPedido.Dulce;
        }

        if (salado && !dulce)
        {
            return SaborPedido.Salado;
        }

        return SaborPedido.Ninguno;
    }

    private static bool EsDulce(RecetaCompatibleDto receta)
    {
        if (string.Equals(receta.Categoria, "postres", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var titulo = receta.Titulo.ToLowerInvariant();
        return PalabrasDulces.Any(palabra => titulo.Contains(palabra, StringComparison.Ordinal));
    }

    private static List<RecetaCompatibleDto> FiltrarPorSabor(List<RecetaCompatibleDto> recetas, SaborPedido sabor)
    {
        if (sabor == SaborPedido.Dulce)
        {
            return recetas.Where(receta => EsDulce(receta) && !TieneBaseSalada(receta.Titulo)).ToList();
        }

        if (sabor == SaborPedido.Salado)
        {
            return recetas.Where(receta => !EsDulce(receta)).ToList();
        }

        return recetas;
    }

    private static readonly CultureInfo CulturaArgentina = CultureInfo.GetCultureInfo("es-AR");

    private static readonly string[] FormatosFecha = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-ddTHH:mm:ss"];

    private static DateTime ParsearFecha(string? fecha)
    {
        if (!string.IsNullOrWhiteSpace(fecha) &&
            DateTime.TryParseExact(fecha.Trim(), FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parseada))
        {
            return parseada.Date;
        }

        return FechaLocal.Hoy();
    }

    private static bool CoincideTitulo(string tituloReceta, string busqueda) =>
        BusquedaDifusa.Coincide(tituloReceta, busqueda);

    private const int MaxTurnosHistorial = 20;

    private static IReadOnlyList<ChatMessageDto> ObtenerTurnos(ChatRequestDto? request)
    {
        if (request?.Mensajes is null || request.Mensajes.Count == 0)
        {
            return [];
        }

        return request.Mensajes
            .Where(turno => !string.IsNullOrWhiteSpace(turno.Texto))
            .TakeLast(MaxTurnosHistorial)
            .ToList();
    }

    private static List<MensajeIa> ConstruirConversacion(string contexto, IReadOnlyList<ChatMessageDto> turnos)
    {
        var mensajes = new List<MensajeIa>
        {
            new("user", contexto)
        };

        foreach (var turno in turnos)
        {
            mensajes.Add(new MensajeIa(NormalizarRolGroq(turno.Rol), turno.Texto.Trim()));
        }

        return mensajes;
    }

    private static string NormalizarRolGroq(string? rol)
    {
        if (string.Equals(rol, "asistente", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rol, "assistant", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rol, "model", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rol, "ia", StringComparison.OrdinalIgnoreCase))
        {
            return "assistant";
        }

        return "user";
    }

    private async Task<string> ConstruirContextoAsync(
        int usuarioId,
        RecetaGeneradaDto? recetaActual,
        string? preferenciaDietetica)
    {
        var stock = await _stockRepository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId && item.Cantidad > 0,
            item => item.Ingrediente);

        var contexto = new StringBuilder();
        contexto.AppendLine("[CONTEXTO PARA EL ASISTENTE]");
        var hoy = FechaLocal.Hoy();
        contexto.AppendLine($"Fecha de hoy: {hoy:yyyy-MM-dd} ({hoy.ToString("dddd", CulturaArgentina)}).");
        contexto.AppendLine($"Preferencia dietética del usuario: {preferenciaDietetica?.Trim() ?? "Ninguna"}.");
        contexto.AppendLine("Ingredientes que el usuario tiene actualmente en su alacena:");

        if (stock.Count == 0)
        {
            contexto.AppendLine("- (la alacena está vacía: si pide una receta, igual generá una sencilla con ingredientes básicos; el sistema le muestra qué le falta)");
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

        var recientes = await ObtenerRecetasRecientesAsync(usuarioId);
        contexto.AppendLine();
        if (recientes.Count == 0)
        {
            contexto.AppendLine("RECETAS RECIENTES: (ninguna todavía)");
        }
        else
        {
            contexto.AppendLine("RECETAS RECIENTES (sin guardar, 1 = la más reciente):");
            for (var indice = 0; indice < recientes.Count; indice++)
            {
                var receta = recientes[indice];
                var ingredientes = string.Join(", ", receta.Ingredientes
                    .Where(ingrediente => !string.IsNullOrWhiteSpace(ingrediente.Nombre))
                    .Select(ingrediente => ingrediente.Nombre.Trim()));
                contexto.AppendLine($"{indice + 1}. {receta.Titulo} ({receta.Dificultad}, {receta.Categoria}) - ingredientes: {ingredientes}");
            }
        }

        var misRecetas = (await _recetaService.GetAllAsync(usuarioId))
            .Where(receta => receta.UsuarioId == usuarioId)
            .OrderByDescending(receta => receta.Id)
            .Take(20)
            .ToList();
        contexto.AppendLine();
        if (misRecetas.Count == 0)
        {
            contexto.AppendLine("MIS RECETAS GUARDADAS: (ninguna todavía)");
        }
        else
        {
            contexto.AppendLine("MIS RECETAS GUARDADAS (disponibles para planificar, cocinar o borrar por nombre):");
            foreach (var receta in misRecetas)
            {
                contexto.AppendLine($"- {receta.Titulo} ({receta.Dificultad}, {receta.Categoria})");
            }
        }

        var planificadas = (await _planificadorService.GetByUsuarioIdAsync(usuarioId))
            .Where(comida => comida.FechaAsignada.Date >= hoy)
            .OrderBy(comida => comida.FechaAsignada)
            .ToList();
        contexto.AppendLine();
        if (planificadas.Count == 0)
        {
            contexto.AppendLine("PLANIFICADOR ACTUAL: (sin comidas próximas)");
        }
        else
        {
            contexto.AppendLine("PLANIFICADOR ACTUAL:");
            foreach (var comida in planificadas)
            {
                contexto.AppendLine(
                    $"- {comida.FechaAsignada:yyyy-MM-dd} ({comida.FechaAsignada.ToString("dddd", CulturaArgentina)}) {comida.Turno}: {comida.RecetaTitulo}");
            }
        }

        var guardadas = await ObtenerRecetasGuardadasRecientesAsync(usuarioId);
        contexto.AppendLine();
        if (guardadas.Count == 0)
        {
            contexto.AppendLine("RECETAS GUARDADAS RECIENTEMENTE: (ninguna todavía)");
        }
        else
        {
            contexto.AppendLine("RECETAS GUARDADAS RECIENTEMENTE (para variar, no para repetir):");
            foreach (var receta in guardadas)
            {
                var ingredientes = string.Join(", ", receta.IngredientesReceta
                    .Select(ingrediente => ingrediente.Ingrediente?.Nombre)
                    .Where(nombre => !string.IsNullOrWhiteSpace(nombre)));
                contexto.AppendLine($"- {receta.Titulo} - ingredientes: {ingredientes}");
            }
        }

        if (stock.Count > 0)
        {
            var usados = recientes
                .SelectMany(receta => receta.Ingredientes.Select(ingrediente => ingrediente.Nombre))
                .Concat(guardadas.SelectMany(receta => receta.IngredientesReceta.Select(ingrediente => ingrediente.Ingrediente?.Nombre)))
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Select(nombre => nombre!.Trim().ToLowerInvariant())
                .ToList();

            var usoPorIngrediente = stock
                .Select(item => item.Ingrediente?.Nombre?.Trim())
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(nombre => (Nombre: nombre!, Usos: usados.Count(usado => CoincideIngrediente(usado, nombre!))))
                .ToList();

            var sinUsar = usoPorIngrediente.Where(item => item.Usos == 0).Select(item => item.Nombre).ToList();
            var yaUsados = usoPorIngrediente
                .Where(item => item.Usos > 0)
                .OrderByDescending(item => item.Usos)
                .Select(item => $"{item.Nombre} ({item.Usos})")
                .ToList();

            contexto.AppendLine();
            contexto.AppendLine("INGREDIENTES DE LA ALACENA SIN USAR EN RECETAS RECIENTES O GUARDADAS (elegí la base del plato entre estos cuando se pueda): " +
                (sinUsar.Count == 0 ? "(ninguno)" : string.Join(", ", sinUsar)));
            contexto.AppendLine("INGREDIENTES DE LA ALACENA YA USADOS (cantidad de recetas; evitá repetirlos como base): " +
                (yaUsados.Count == 0 ? "(ninguno)" : string.Join(", ", yaUsados)));
        }

        var recetaDetalle = EsRecetaValida(recetaActual) ? recetaActual : recientes.FirstOrDefault();
        if (recetaDetalle is not null)
        {
            contexto.AppendLine();
            contexto.AppendLine(EsRecetaValida(recetaActual)
                ? "Receta que el usuario tiene abierta en pantalla ('esta'):"
                : "Detalle de la receta 1:");
            contexto.AppendLine(JsonSerializer.Serialize(recetaDetalle, JsonOptions));
        }

        return contexto.ToString();
    }

    private const int MaxReintentosVariedad = 2;

    private async Task<SobreAgente> EvitarRecetaRepetidaAsync(
        int usuarioId,
        SobreAgente sobre,
        List<MensajeIa> mensajes,
        string systemInstruction,
        string pedidoUsuario)
    {
        if (sobre.Receta is null || sobre.Receta.Ingredientes.Count == 0)
        {
            return sobre;
        }

        var anteriores = (await ObtenerRecetasRecientesAsync(usuarioId))
            .Select(receta => (receta.Titulo, Ingredientes: receta.Ingredientes.Select(ingrediente => ingrediente.Nombre).ToList()))
            .Concat((await ObtenerRecetasGuardadasRecientesAsync(usuarioId))
                .Select(receta => (receta.Titulo, Ingredientes: receta.IngredientesReceta
                    .Select(ingrediente => ingrediente.Ingrediente?.Nombre ?? string.Empty)
                    .ToList())))
            .ToList();

        var actual = sobre;
        var rechazadas = new List<string>();

        for (var intento = 0; intento < MaxReintentosVariedad; intento++)
        {
            var nueva = IngredientesPrincipales(actual.Receta!.Ingredientes.Select(ingrediente => ingrediente.Nombre));
            var parecida = anteriores.FirstOrDefault(anterior =>
                EsRecetaParecida(nueva, IngredientesPrincipales(anterior.Ingredientes), pedidoUsuario));

            if (parecida.Titulo is null)
            {
                return actual;
            }

            rechazadas.Add(actual.Receta.Titulo);
            var listaRechazadas = string.Join(", ", rechazadas.Select(titulo => $"\"{titulo}\""));
            var conversacion = new List<MensajeIa>(mensajes)
            {
                new("user",
                    $"La receta \"{actual.Receta.Titulo}\" se parece demasiado a \"{parecida.Titulo}\" " +
                    $"(misma base o casi los mismos ingredientes). Ya rechacé: {listaRechazadas}. " +
                    "Generá otra receta real con OTRO ingrediente base y OTRO tipo de preparación. " +
                    $"Si la alacena no alcanza, comprá como máximo {RealismoRecetaPrompt.MaxIngredientesAComprar} ingredientes. " +
                    "Respondé con accion 'generar_receta'.")
            };

            try
            {
                var json = await _asistenteIa.GenerarRespuestaConversacionAsync(conversacion, systemInstruction, jsonMode: true);
                var alternativa = DeserializarSobre(json);
                if (alternativa?.Receta is { Ingredientes.Count: > 0 } &&
                    string.Equals(alternativa.Accion?.Trim(), ChatAccion.GenerarReceta, StringComparison.OrdinalIgnoreCase))
                {
                    actual = alternativa;
                    continue;
                }

                return sobre;
            }
            catch (ServicioExternoException)
            {
                return sobre;
            }
        }

        return actual;
    }

    private static readonly string[] BasicosDeDespensa = ["sal", "pimienta", "aceite", "agua"];

    private const double SimilitudMaximaIngredientes = 0.6;

    private static List<string> IngredientesPrincipales(IEnumerable<string?> nombres) =>
        nombres
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Select(nombre => nombre!.Trim().ToLowerInvariant())
            .Where(nombre => !BasicosDeDespensa.Any(basico => nombre.StartsWith(basico, StringComparison.Ordinal)))
            .Distinct()
            .ToList();

    private static bool EsRecetaParecida(List<string> nueva, List<string> anterior, string pedidoUsuario)
    {
        if (nueva.Count == 0 || anterior.Count == 0)
        {
            return false;
        }

        if (pedidoUsuario.ToLowerInvariant().Contains(anterior[0], StringComparison.Ordinal))
        {
            return false;
        }

        if (CoincideIngrediente(nueva[0], anterior[0]))
        {
            return true;
        }

        var compartidos = nueva.Count(ingrediente => anterior.Any(otro => CoincideIngrediente(ingrediente, otro)));
        var union = nueva.Count + anterior.Count - compartidos;
        return union > 0 && (double)compartidos / union >= SimilitudMaximaIngredientes;
    }

    private static bool CoincideIngrediente(string usado, string enAlacena)
    {
        var alacena = enAlacena.Trim().ToLowerInvariant();
        return usado.Contains(alacena, StringComparison.Ordinal) || alacena.Contains(usado, StringComparison.Ordinal);
    }

    private async Task<List<Receta>> ObtenerRecetasGuardadasRecientesAsync(int usuarioId)
    {
        var favoritos = await _favoritoRepository.FindWithIncludePathsAsync(
            favorito => favorito.UsuarioId == usuarioId,
            $"{nameof(RecetaFavorita.Receta)}.{nameof(Receta.IngredientesReceta)}.{nameof(IngredienteReceta.Ingrediente)}");

        return favoritos
            .Where(favorito => favorito.Receta is not null)
            .OrderByDescending(favorito => favorito.Id)
            .Take(MaxRecetasGuardadasEnContexto)
            .Select(favorito => favorito.Receta)
            .ToList();
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

        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        [JsonPropertyName("ingredientes")]
        public List<IngredienteGeneradoDto>? Ingredientes { get; set; }

        [JsonPropertyName("pasos")]
        public List<string>? Pasos { get; set; }

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

        [JsonPropertyName("ingredientesAlacena")]
        public List<IngredienteAlacenaChatDto>? IngredientesAlacena { get; set; }

        [JsonPropertyName("eliminarTodosAlacena")]
        public bool EliminarTodosAlacena { get; set; }

        [JsonPropertyName("indiceReceta")]
        public int? IndiceReceta { get; set; }

        [JsonPropertyName("reemplazarSiConflicto")]
        public bool ReemplazarSiConflicto { get; set; }
    }

    private sealed class IngredienteAlacenaChatDto
    {
        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        [JsonPropertyName("cantidad")]
        public decimal? Cantidad { get; set; }

        [JsonPropertyName("unidadMedida")]
        public string? UnidadMedida { get; set; }
    }
}
