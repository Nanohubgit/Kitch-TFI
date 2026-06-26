using Kitch.Application.DTOs.Planificador;
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

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetByUsuarioId(int usuarioId)
    {
        if (!TryGetUsuarioId(out var usuarioActualId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var comidas = await _planificadorService.GetByUsuarioIdAsync(usuarioId);
        return Ok(comidas);
    }

    [HttpGet("usuario/{usuarioId:int}/fecha")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetByFecha(
        int usuarioId,
        [FromQuery] DateTime fecha)
    {
        if (!TryGetUsuarioId(out var usuarioActualId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var comidas = await _planificadorService.GetByFechaAsync(usuarioId, fecha);
        return Ok(comidas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> GetById(int id)
    {
        var comida = await _planificadorService.GetByIdAsync(id);

        if (comida is null)
        {
            return NotFound();
        }

        return Ok(comida);
    }

    [HttpPost]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> Create([FromBody] ComidaPlanificadaCreateDto comida)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        comida.UsuarioId = usuarioId;

        try
        {
            var createdComida = await _planificadorService.CreateAsync(comida);
            return CreatedAtAction(nameof(GetById), new { id = createdComida.Id }, createdComida);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ComidaPlanificadaUpdateDto comida)
    {
        var updated = await _planificadorService.UpdateAsync(id, comida);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _planificadorService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
