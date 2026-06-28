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

    [HttpPost("enviar")]
    public async Task<IActionResult> EnviarMensaje([FromBody] ChatRequestDto request)
    {
        // El usuario sale del token JWT, no del body: nadie puede chatear haciéndose pasar por otro.
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var resultado = await _chatIaService.EnviarMensajeChatAsync(usuarioId, request.Mensaje);
        return Ok(new { Respuesta = resultado });
    }
}

public class ChatRequestDto
{
    public string Mensaje { get; set; } = string.Empty;
}
