using System.Text;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kitch.Application.Services;

public class ChatIaService : IChatIaService
{
    // Ley suprema por defecto: si el appsettings no la define, igual blindamos el asistente
    // para que solo responda temas de cocina dentro del dominio de Kitch.
    private const string InstruccionPorDefecto =
        "Sos el asistente de cocina exclusivo de Kitch. Únicamente respondés consultas " +
        "relacionadas con cocina, recetas, ingredientes, técnicas culinarias y planificación " +
        "de comidas. Conocés la alacena del usuario (te la pasamos como contexto) y la usás " +
        "para recomendar qué cocinar con lo que tiene, avisando qué le falta. Si el usuario " +
        "quiere una receta completa para guardar, recordale que puede pedirla con la opción " +
        "'generar receta'. Si te preguntan algo ajeno a la cocina, rechazá la consulta con " +
        "amabilidad. Estás hablando con {0}.";

    private const string NombrePorDefecto = "Chef";

    private readonly IGeminiClient _geminiClient;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IConfiguration _configuration;

    public ChatIaService(
        IGeminiClient geminiClient,
        IRepository<Usuario> usuarioRepository,
        IRepository<StockUsuario> stockRepository,
        IConfiguration configuration)
    {
        _geminiClient = geminiClient;
        _usuarioRepository = usuarioRepository;
        _stockRepository = stockRepository;
        _configuration = configuration;
    }

    public async Task<string> EnviarMensajeChatAsync(int usuarioId, string mensajeUsuario)
    {
        // a) Resolvemos el nombre del usuario; si no existe usamos "Chef" por defecto.
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        var nombreUsuario = string.IsNullOrWhiteSpace(usuario?.Nombre)
            ? NombrePorDefecto
            : usuario!.Nombre;

        // b) Leemos la restricción de cocina desde el appsettings; si está vacía, aplicamos la estricta por defecto.
        var instruccionBase = _configuration["Gemini:SystemInstruction"];
        if (string.IsNullOrWhiteSpace(instruccionBase))
        {
            instruccionBase = InstruccionPorDefecto;
        }

        // c) Inyectamos el nombre del usuario dentro del System Instruction.
        var instruccionFormateada = string.Format(instruccionBase, nombreUsuario);

        // d) Sumamos la alacena del usuario como contexto para que la IA pueda recomendar.
        var contexto = await ConstruirContextoAlacenaAsync(usuarioId);
        var prompt = $"{contexto}\n\nMensaje del usuario: {mensajeUsuario}";

        // e) Delegamos en el cliente de Gemini y devolvemos la respuesta de la IA.
        return await _geminiClient.GenerarRespuestaAsync(prompt, instruccionFormateada);
    }

    private async Task<string> ConstruirContextoAlacenaAsync(int usuarioId)
    {
        var stock = await _stockRepository.FindWithIncludesAsync(
            item => item.UsuarioId == usuarioId && item.Cantidad > 0,
            item => item.Ingrediente);

        var contexto = new StringBuilder();
        contexto.AppendLine("Ingredientes que el usuario tiene actualmente en su alacena:");

        if (stock.Count == 0)
        {
            contexto.AppendLine("- (la alacena está vacía)");
            return contexto.ToString();
        }

        foreach (var item in stock)
        {
            var nombre = string.IsNullOrWhiteSpace(item.Ingrediente?.Nombre)
                ? $"Ingrediente #{item.IngredienteId}"
                : item.Ingrediente!.Nombre;
            contexto.AppendLine($"- {nombre}: {item.Cantidad} {item.UnidadMedida}");
        }

        return contexto.ToString();
    }
}
