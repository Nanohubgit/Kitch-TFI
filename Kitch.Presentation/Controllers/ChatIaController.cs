using Kitch.Application.DTOs.RecetaIa;
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
    private readonly IRecetaIaService _recetaIaService;

    public ChatIaController(IChatIaService chatIaService, IRecetaIaService recetaIaService)
    {
        _chatIaService = chatIaService;
        _recetaIaService = recetaIaService;
    }

    // Chat unificado: conversa Y conoce la alacena del usuario para recomendar.
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

    // Paso 1: la IA genera una receta usando la alacena. NO se guarda todavía (es un borrador).
    [HttpPost("generar-receta")]
    public async Task<ActionResult<RecetaGeneradaDto>> GenerarReceta([FromBody] GenerarRecetaRequest request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var receta = await _recetaIaService.GenerarRecetaAsync(usuarioId, request?.Preferencias);
            return Ok(receta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Paso 2: el usuario decide guardar la receta generada. Se persiste y se marca como favorita.
    [HttpPost("guardar-receta")]
    public async Task<ActionResult<RecetaGuardadaResponse>> GuardarReceta([FromBody] RecetaGeneradaDto receta)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var resultado = await _recetaIaService.GuardarRecetaAsync(usuarioId, receta);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class ChatRequestDto
{
    public string Mensaje { get; set; } = string.Empty;
}
