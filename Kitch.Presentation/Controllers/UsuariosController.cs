using Kitch.Application.DTOs.Usuarios;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ApiControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetAll()
    {
        var adminId = GetUsuarioIdOrThrow();
        var usuarios = await _usuarioService.GetAllAsync(adminId);
        return Ok(usuarios);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioResponseDto>> GetById(int id)
    {
        var adminId = GetUsuarioIdOrThrow();
        var usuario = await _usuarioService.GetByIdAdminAsync(id, adminId);

        if (usuario is null)
        {
            return NotFound();
        }

        return Ok(usuario);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioResponseDto>> Create([FromBody] UsuarioCreateDto usuario)
    {
        var adminId = GetUsuarioIdOrThrow();
        var createdUsuario = await _usuarioService.CreateAsync(usuario, adminId);
        return CreatedAtAction(nameof(GetById), new { id = createdUsuario.Id }, createdUsuario);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDto usuario)
    {
        var adminId = GetUsuarioIdOrThrow();
        var updated = await _usuarioService.UpdateAsync(id, usuario, adminId);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id:int}/cambiar-rol")]
    public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolDto request)
    {
        var adminId = GetUsuarioIdOrThrow("No se pudo identificar al administrador a partir del token.");
        var actualizado = await _usuarioService.CambiarRolAsync(id, request.NuevoRol, adminId);

        if (!actualizado)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var adminId = GetUsuarioIdOrThrow("No se pudo identificar al administrador a partir del token.");
        var deleted = await _usuarioService.DeleteAsync(id, adminId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
