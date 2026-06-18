using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuscripcionesController : ApiControllerBase
{
    private readonly ISuscripcionService _suscripcionService;

    public SuscripcionesController(ISuscripcionService suscripcionService)
    {
        _suscripcionService = suscripcionService;
    }

    [HttpPost("contratar")]
    public async Task<ActionResult<ContratarSuscripcionResult>> Contratar([FromBody] ContratarSuscripcionRequest request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var result = await _suscripcionService.ContratarAsync(usuarioId, request);

        if (!result.Aprobado)
        {
            return StatusCode(StatusCodes.Status402PaymentRequired, result);
        }

        return Ok(result);
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
