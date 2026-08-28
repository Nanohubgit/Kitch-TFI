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

    [HttpGet("mias")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetMias()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var comidas = await _planificadorService.GetByUsuarioIdAsync(usuarioId);
        return Ok(comidas);
    }

    [HttpGet("mias/fecha")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetMiasPorFecha(
        [FromQuery] DateTime fecha)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var comidas = await _planificadorService.GetByFechaAsync(usuarioId, fecha);
        return Ok(comidas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> GetById(int id)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var comida = await _planificadorService.GetByIdAsync(id, usuarioId);

        if (comida is null)
        {
            return NotFound();
        }

        return Ok(comida);
    }

    [HttpPost]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> Create([FromBody] ComidaPlanificadaCreateDto comida)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        comida.UsuarioId = usuarioId;

        try
        {
            var createdComida = await _planificadorService.CreateAsync(comida);
            return CreatedAtAction(nameof(GetById), new { id = createdComida.Id }, createdComida);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ComidaPlanificadaUpdateDto comida)
    {
        var usuarioId = GetUsuarioIdOrThrow();

        try
        {
            var updated = await _planificadorService.UpdateAsync(id, comida, usuarioId);

            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var deleted = await _planificadorService.DeleteAsync(id, usuarioId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
