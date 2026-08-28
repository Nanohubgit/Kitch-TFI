using Kitch.Application.DTOs.Usuarios;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PerfilController : ApiControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public PerfilController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UsuarioResponseDto>> GetMe()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var usuario = await _usuarioService.GetByIdAsync(usuarioId);

        if (usuario is null)
        {
            return NotFound();
        }

        return Ok(usuario);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] ActualizarPerfilDto perfil)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var actualizado = await _usuarioService.ActualizarPerfilAsync(usuarioId, perfil);

        if (!actualizado)
        {
            return NotFound();
        }

        return NoContent();
    }
}
