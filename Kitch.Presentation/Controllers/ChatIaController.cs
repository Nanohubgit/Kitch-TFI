using Kitch.Application.DTOs.ChatIa;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatIaController : ApiControllerBase
{
    private readonly IChatIaService _chatIaService;

    public ChatIaController(IChatIaService chatIaService)
    {
        _chatIaService = chatIaService;
    }

    // Endpoint ÚNICO del asistente. Según lo que el usuario pida en lenguaje natural, el
    // agente conversa, genera una receta, la guarda (favoritos), sugiere sustitutos o
    // recomienda recetas, y ejecuta los efectos correspondientes. Todo en la misma conversación.
    [HttpPost]
    public async Task<ActionResult<ChatRespuestaDto>> Chat([FromBody] ChatRequestDto request)
    {
        // El usuario sale del token JWT, no del body: nadie puede chatear haciéndose pasar por otro.
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var respuesta = await _chatIaService.ProcesarMensajeAsync(usuarioId, request);
            return Ok(respuesta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
