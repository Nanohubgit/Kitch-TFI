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
        var usuario = await _usuarioService.GetByIdAsync(GetUsuarioIdOrThrow());
        if (usuario is null)
        {
            throw new KeyNotFoundException("Usuario no encontrado.");
        }

        return Ok(usuario);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] ActualizarPerfilDto perfil)
    {
        var actualizado = await _usuarioService.ActualizarPerfilAsync(GetUsuarioIdOrThrow(), perfil);
        if (!actualizado)
        {
            throw new KeyNotFoundException("Usuario no encontrado.");
        }

        return NoContent();
    }
}
