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
        "de comidas. Si te preguntan algo ajeno a la cocina, rechazá la consulta con amabilidad " +
        "y recordá que solo podés ayudar con temas culinarios. Estás hablando con {0}.";

    private const string NombrePorDefecto = "Chef";

    private readonly IGeminiClient _geminiClient;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IConfiguration _configuration;

    public ChatIaService(
        IGeminiClient geminiClient,
        IRepository<Usuario> usuarioRepository,
        IConfiguration configuration)
    {
        _geminiClient = geminiClient;
        _usuarioRepository = usuarioRepository;
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

        // d) Delegamos en el cliente de Gemini y devolvemos la respuesta de la IA.
        return await _geminiClient.GenerarRespuestaAsync(mensajeUsuario, instruccionFormateada);
    }
}
