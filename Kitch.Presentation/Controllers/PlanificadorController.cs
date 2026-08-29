using Kitch.Application.DTOs.Planificador;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanificadorController : ApiControllerBase
{
    private readonly IPlanificadorService _planificadorService;

    public PlanificadorController(IPlanificadorService planificadorService)
    {
        _planificadorService = planificadorService;
    }

    [HttpGet("mias")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetMias()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var comidas = await _planificadorService.GetByUsuarioIdAsync(usuarioId);
        return Ok(comidas);
    }

    [HttpGet("mias/fecha")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetMiasPorFecha(
        [FromQuery] DateTime fecha)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var comidas = await _planificadorService.GetByFechaAsync(usuarioId, fecha);
        return Ok(comidas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> GetById(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var comida = await _planificadorService.GetByIdAsync(id, usuarioId);
        if (comida is null)
        {
            return NotFound(new { message = "Comida planificada no encontrada." });
        }

        return Ok(comida);
    }

    [HttpPost]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> Create([FromBody] ComidaPlanificadaCreateDto comida)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        comida.UsuarioId = usuarioId;

        try
        {
            var createdComida = await _planificadorService.CreateAsync(comida);
            return CreatedAtAction(nameof(GetById), new { id = createdComida.Id }, createdComida);
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenMessage(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ComidaPlanificadaUpdateDto comida)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var updated = await _planificadorService.UpdateAsync(id, comida, usuarioId);
            if (!updated)
            {
                return NotFound(new { message = "Comida planificada no encontrada." });
            }

            return NoContent();
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenMessage(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var deleted = await _planificadorService.DeleteAsync(id, usuarioId);
        if (!deleted)
        {
            return NotFound(new { message = "Comida planificada no encontrada." });
        }

        return NoContent();
    }
}
