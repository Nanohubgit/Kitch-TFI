using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuscripcionesController : ControllerBase
{
    private readonly ISuscripcionService _suscripcionService;

    public SuscripcionesController(ISuscripcionService suscripcionService)
    {
        _suscripcionService = suscripcionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Suscripcion>>> GetAll()
    {
        var suscripciones = await _suscripcionService.GetAllAsync();
        return Ok(suscripciones);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Suscripcion>> GetById(int id)
    {
        var suscripcion = await _suscripcionService.GetByIdAsync(id);

        if (suscripcion is null)
        {
            return NotFound();
        }

        return Ok(suscripcion);
    }

    [HttpPost]
    public async Task<ActionResult<Suscripcion>> Create([FromBody] Suscripcion suscripcion)
    {
        var createdSuscripcion = await _suscripcionService.CreateAsync(suscripcion);
        return CreatedAtAction(nameof(GetById), new { id = createdSuscripcion.Id }, createdSuscripcion);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Suscripcion suscripcion)
    {
        var updated = await _suscripcionService.UpdateAsync(id, suscripcion);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _suscripcionService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
