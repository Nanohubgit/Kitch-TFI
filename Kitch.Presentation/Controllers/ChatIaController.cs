using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatIaController : ControllerBase
{
    private readonly IChatIaService _chatIaService;

    public ChatIaController(IChatIaService chatIaService)
    {
        _chatIaService = chatIaService;
    }

    [HttpPost("enviar")]
    public async Task<IActionResult> EnviarMensaje([FromBody] ChatRequestDto request)
    {
        var resultado = await _chatIaService.EnviarMensajeChatAsync(request.UsuarioId, request.Mensaje);
        return Ok(new { Respuesta = resultado });
    }
}

public class ChatRequestDto
{
    public int UsuarioId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
