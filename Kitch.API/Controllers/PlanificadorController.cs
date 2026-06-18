using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanificadorController : ControllerBase
{
    private readonly IPlanificadorService _planificadorService;

    public PlanificadorController(IPlanificadorService planificadorService)
    {
        _planificadorService = planificadorService;
    }

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificada>>> GetByUsuarioId(int usuarioId)
    {
        var comidas = await _planificadorService.GetByUsuarioIdAsync(usuarioId);
        return Ok(comidas);
    }

    [HttpGet("usuario/{usuarioId:int}/fecha")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificada>>> GetByFecha(
        int usuarioId,
        [FromQuery] DateTime fecha)
    {
        var comidas = await _planificadorService.GetByFechaAsync(usuarioId, fecha);
        return Ok(comidas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ComidaPlanificada>> GetById(int id)
    {
        var comida = await _planificadorService.GetByIdAsync(id);

        if (comida is null)
        {
            return NotFound();
        }

        return Ok(comida);
    }

    [HttpPost]
    public async Task<ActionResult<ComidaPlanificada>> Create([FromBody] ComidaPlanificada comida)
    {
        var createdComida = await _planificadorService.CreateAsync(comida);
        return CreatedAtAction(nameof(GetById), new { id = createdComida.Id }, createdComida);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ComidaPlanificada comida)
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
