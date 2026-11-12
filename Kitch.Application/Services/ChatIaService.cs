using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kitch.Application.Common;
using Kitch.Application.DTOs.ChatIa;
using Kitch.Application.DTOs.Planificador;
using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.DTOs.Recomendacion;
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
        "Únicamente respondés temas de cocina: recetas, ingredientes, técnicas culinarias y planificación de comidas. " +
        "Si te preguntan algo ajeno a la cocina, rechazalo con amabilidad (accion 'conversar'). " +
        "Conocés la alacena del usuario (te la pasamos como contexto) y la usás para recomendar y avisar qué le falta. " +
        "Toda receta, sustitución o recomendación DEBE respetar la restricción dietética del bloque anterior.\n\n" +
        "MEMORIA: tenés el historial de esta conversación y, en el contexto, la lista 'RECETAS RECIENTES' numerada " +
        "(1 = la más reciente). Entendé referencias temporales e implícitas: 'la anterior', 'la última', 'esa', 'guardala' → 1; " +
        "'la anteúltima', 'la de antes', 'la primera que me diste' → el número que corresponda; 'la de fideos con papa' → la que tenga ese plato. " +
        "Cuando el usuario se refiera a una receta de esa lista, poné su número en 'indiceReceta' y su título en 'nombre'. " +
        "Si hay varias recetas, identificá CUÁL pidió el usuario; no inventes otra.\n\n" +
        "RESPONDÉS SIEMPRE EXCLUSIVAMENTE con un ÚNICO objeto JSON válido, sin texto extra ni markdown, con esta forma EXACTA:\n" +
        "{\"accion\": \"conversar\"|\"generar_receta\"|\"guardar_receta\"|\"sustituir\"|\"recomendar\"|\"eliminar_receta\"|\"planificar_receta\"|\"cocinar_receta\"|\"consultar_recetas_guardadas\", " +
        "\"mensaje\": string, " +
        "\"nombre\": string | null, " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}] | null, " +
        "\"pasos\": [string] | null, " +
        "\"receta\": {\"titulo\": string, \"descripcion\": string, \"tiempoPreparacionMinutos\": number, " +
        "\"porciones\": number, \"dificultad\": \"Facil\"|\"Medio\"|\"Dificil\", \"caloriasEstimadas\": number, " +
        "\"categoria\": \"pastas\"|\"carnes\"|\"pollo\"|\"ensaladas\"|\"sopas\"|\"pescados\"|\"pizzas\"|\"postres\"|\"tartas\"|\"guisos\"|\"general\", " +
        "\"ingredientes\": [{\"nombre\": string, \"cantidad\": number, \"unidadMedida\": string}], \"pasos\": [string]} | null, " +
        "\"ingredienteSustituir\": string | null, \"tituloEliminar\": string | null, \"eliminarTodas\": boolean, " +
        "\"tituloPlanificar\": string | null, \"fechaPlanificar\": string | null, \"turnoPlanificar\": string | null, " +
        "\"tituloCocinar\": string | null, \"porcionesCocinar\": number | null, " +
        "\"indiceReceta\": number | null, \"reemplazarSiConflicto\": boolean}\n\n" +
        "REGLAS DE LA 'accion':\n" +
        "- 'conversar': charla general, dudas, consejos. 'receta' va en null.\n" +
        "- 'generar_receta': el usuario pide una receta. Completás 'receta' (mín. 1 ingrediente y 1 paso, " +
        "tiempo y porciones > 0). Usá preferentemente lo que hay en la alacena. En 'mensaje' presentás la receta. " +
        "SIEMPRE poné en 'titulo' un nombre descriptivo y real del plato (ej. 'Tortilla de papas'); NUNCA uses 'string', " +
        "'receta' ni dejes el título vacío. Si el usuario no aclara el nombre, inventá uno acorde a los ingredientes. " +
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
        "En 'tituloPlanificar' poné el nombre de la receta (o dejalo null si se refiere a la última receta de la conversación); " +
        "si es una de RECETAS RECIENTES, poné además su número en 'indiceReceta'. " +
        "Si el sistema avisó que ese turno ya estaba ocupado y el usuario confirma que quiere reemplazarlo ('sí, reemplazala'), " +
        "repetí receta, fecha y turno y poné 'reemplazarSiConflicto' en true; en cualquier otro caso dejalo en false. " +
        "En 'fechaPlanificar' poné la fecha en formato YYYY-MM-DD (resolvé 'hoy', 'mañana', 'el viernes' usando la 'Fecha de hoy' del contexto). " +
        "En 'turnoPlanificar' poné el momento: 'Desayuno', 'Almuerzo', 'Merienda' o 'Cena'. " +
        "En 'mensaje' confirmás. El sistema agrega solo lo que el usuario tenga guardado y, si le faltan ingredientes, " +
        "los suma automáticamente a su lista de compras.\n" +
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

    private const int MaxRecetasRecientes = 5;

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
        IUnidadDeTrabajo unidadDeTrabajo,
        IMemoryCache cache)
    {
        _recetaService = recetaService;
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
            .Replace("__NOMBRE_USUARIO__", nombreUsuario);

        if (!RolUsuario.TieneAccesoPremium(usuario?.Rol))
        {
            systemInstruction +=
                "\n\n" + RecetaIaService.InstruccionPlanBasico +
                " Si el plato pedido solo puede salir difícil, usá 'conversar', explicá que es del plan Profesional " +
                "y ofrecé una alternativa realmente simple.";
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

        var respuesta = (sobre.Accion ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            ChatAccion.GenerarReceta => await ResolverGenerarRecetaAsync(usuarioId, sobre),
            ChatAccion.GuardarReceta => await ResolverGuardarRecetaAsync(usuarioId, sobre, request.RecetaActual),
            ChatAccion.Sustituir => await ResolverSustituirAsync(usuarioId, sobre),
            ChatAccion.Recomendar => await ResolverRecomendarAsync(usuarioId, sobre),
            ChatAccion.EliminarReceta => await ResolverEliminarRecetaAsync(usuarioId, sobre),
            ChatAccion.PlanificarReceta => await ResolverPlanificarRecetaAsync(usuarioId, sobre),
            ChatAccion.CocinarReceta => await ResolverCocinarRecetaAsync(usuarioId, sobre),
            ChatAccion.ConsultarRecetasGuardadas => await ResolverConsultarRecetasGuardadasAsync(usuarioId, sobre),
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
        var receta = await ElegirRecetaParaGuardarAsync(usuarioId, sobre, recetaActual);

        if (receta is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = "No encuentro una receta para guardar. Primero pedime que te genere una y después decime que la guarde."
            };
        }

        receta.Titulo = RecetaIaService.GenerarTituloPorDefecto(receta.Titulo, receta.Ingredientes);

        try
        {
            await _favoritoService.AsegurarCupoFavoritosAsync(usuarioId);
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

    private async Task<ChatRespuestaDto> ResolverRecomendarAsync(int usuarioId, SobreAgente sobre)
    {
        var candidatas = (await _recomendacionService.RecomendarAsync(usuarioId, LimitesPlan.MaxFaltantesRecomendacion)).ToList();
        var completas = candidatas.Where(receta => receta.Grupo == GrupoRecomendacion.Completa).ToList();
        var recomendaciones = completas.Count > 0 ? completas : candidatas;

        var mensajeSistema = recomendaciones.Count == 0
            ? "No encontré recetas que puedas hacer con lo que tenés (ni faltando hasta 2 ingredientes). Cargá más ingredientes o pedime que te genere una."
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

        var favoritos = await ObtenerFavoritosAccesiblesAsync(usuarioId);

        var favoritosABorrar = favoritos.ToList();
        if (!sobre.EliminarTodas)
        {
            var exactos = favoritos
                .Where(favorito => favorito.Receta is not null &&
                    favorito.Receta.Titulo.Trim().Equals(titulo, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var parciales = favoritos
                .Where(favorito => favorito.Receta is not null && CoincideTitulo(favorito.Receta.Titulo, titulo!))
                .ToList();

            if (exactos.Count == 0 && parciales.Count > 1)
            {
                return new ChatRespuestaDto
                {
                    Accion = ChatAccion.Conversar,
                    Mensaje = $"Encontré varias recetas guardadas que coinciden con \"{titulo}\": " +
                        $"{string.Join(", ", parciales.Select(favorito => $"\"{favorito.Receta.Titulo}\""))}. ¿Cuál querés borrar?"
                };
            }

            favoritosABorrar = exactos.Count > 0 ? exactos : parciales;
        }

        if (favoritosABorrar.Count == 0)
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

        var recetasPropias = favoritosABorrar
            .Where(favorito => favorito.Receta.UsuarioId == usuarioId)
            .Select(favorito => favorito.RecetaId)
            .ToHashSet();

        await _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            foreach (var favorito in favoritosABorrar.Where(favorito => !recetasPropias.Contains(favorito.RecetaId)))
            {
                await _favoritoRepository.DeleteAsync(favorito);
            }

            foreach (var recetaId in recetasPropias)
            {
                await _recetaService.DeleteAsync(recetaId, usuarioId);
            }
        });
        _cache.Remove(RecetaService.CacheCatalogoKey);
        return new ChatRespuestaDto
        {
            Accion = ChatAccion.EliminarReceta,
            Mensaje = string.IsNullOrWhiteSpace(sobre.Mensaje)
                ? (sobre.EliminarTodas
                    ? $"Listo, borré tus {favoritosABorrar.Count} receta(s) guardada(s)."
                    : $"Listo, borré {favoritosABorrar.Count} receta(s) que coincidían con \"{titulo}\".")
                : sobre.Mensaje,
            RecetasEliminadas = favoritosABorrar.Count
        };
    }

    private async Task<ChatRespuestaDto> ResolverPlanificarRecetaAsync(int usuarioId, SobreAgente sobre)
    {
        var (recetaId, tituloReceta, errorReceta) = await ResolverRecetaParaAccionAsync(
            usuarioId,
            sobre.TituloPlanificar,
            sobre.IndiceReceta,
            "planificarla");
        if (recetaId is null)
        {
            return new ChatRespuestaDto
            {
                Accion = ChatAccion.Conversar,
                Mensaje = errorReceta!
            };
        }

        var fecha = ParsearFecha(sobre.FechaPlanificar);

        var turno = InterpretarTurno(sobre.TurnoPlanificar);

        try
        {
            var resultado = await _planificadorService.PlanificarAsync(new ComidaPlanificadaCreateDto
            {
                UsuarioId = usuarioId,
                RecetaId = recetaId.Value,
                FechaAsignada = fecha,
                Turno = turno,
                ReemplazarSiConflicto = sobre.ReemplazarSiConflicto
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

        if (indiceReceta is null && !string.IsNullOrWhiteSpace(titulo))
        {
            var favoritos = await ObtenerFavoritosAccesiblesAsync(usuarioId);

            var receta = favoritos
                .Select(favorito => favorito.Receta)
                .FirstOrDefault(r => r is not null && r.Titulo.Trim().Equals(titulo, StringComparison.OrdinalIgnoreCase))
                ?? favoritos
                    .Select(favorito => favorito.Receta)
                    .FirstOrDefault(r => r is not null && CoincideTitulo(r.Titulo, titulo));

            if (receta is not null)
            {
                return (receta.Id, receta.Titulo, null);
            }
        }

        var recordada = BuscarEnRecientes(recientes, indiceReceta, titulo);
        if (recordada is null && indiceReceta is null && string.IsNullOrWhiteSpace(titulo))
        {
            recordada = recientes.FirstOrDefault();
        }

        if (recordada is null)
        {
            return (null, null, string.IsNullOrWhiteSpace(titulo)
                ? "No sé a qué receta te referís. Generá o guardá una receta primero, o decime el nombre de una que ya tengas guardada."
                : $"No encontré \"{titulo}\" entre tus recetas guardadas ni entre las últimas que te propuse.");
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

    private static string InterpretarTurno(string? turno)
    {
        var texto = (turno ?? string.Empty).Trim().ToLowerInvariant();
        if (texto.Length == 0)
        {
            return TurnosComida.Almuerzo;
        }

        if (texto.Contains("desayun") || texto.Contains("mañana") || texto.Contains("manana"))
        {
            return TurnosComida.Desayuno;
        }

        if (texto.Contains("merienda") || texto.Contains("tarde"))
        {
            return TurnosComida.Merienda;
        }

        if (texto.Contains("cena") || texto.Contains("noche"))
        {
            return TurnosComida.Cena;
        }

        return TurnosComida.Almuerzo;
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

    private static bool CoincideTitulo(string tituloReceta, string busqueda)
    {
        var a = tituloReceta.Trim();
        var b = busqueda.Trim();

        return a.Equals(b, StringComparison.OrdinalIgnoreCase) ||
            a.Contains(b, StringComparison.OrdinalIgnoreCase) ||
            b.Contains(a, StringComparison.OrdinalIgnoreCase);
    }

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

        [JsonPropertyName("indiceReceta")]
        public int? IndiceReceta { get; set; }

        [JsonPropertyName("reemplazarSiConflicto")]
        public bool ReemplazarSiConflicto { get; set; }
    }
}
