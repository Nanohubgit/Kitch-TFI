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

    [HttpPost]
    public async Task<ActionResult<ChatRespuestaDto>> Chat([FromBody] ChatRequestDto request)
    {
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
