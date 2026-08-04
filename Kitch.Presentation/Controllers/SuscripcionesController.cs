using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

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

    /// <summary>
    /// Contrata la suscripción Profesional: cobra vía pasarela y, si aprueba, actualiza el rol.
    /// 200 OK | 400 Bad Request | 401 Unauthorized | 403 Forbidden
    /// </summary>
    [HttpPost("contratar")]
    [Authorize(Roles = RolUsuario.Basico)]
    public async Task<ActionResult<ContratarSuscripcionResult>> Contratar(
        [FromBody] ContratarSuscripcionRequest request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var result = await _suscripcionService.ContratarAsync(usuarioId, request);

            if (!result.Aprobado)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (EsUsuarioYaProfesional(ex))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static bool EsUsuarioYaProfesional(InvalidOperationException ex) =>
        ex.Message.Contains("ya posee el rol Profesional", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SuscripcionResponseDto>>> GetAll()
    {
        var suscripciones = await _suscripcionService.GetAllAsync();
        return Ok(suscripciones);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SuscripcionResponseDto>> GetById(int id)
    {
        var suscripcion = await _suscripcionService.GetByIdAsync(id);

        if (suscripcion is null)
        {
            return NotFound();
        }

        return Ok(suscripcion);
    }

    [HttpPost]
    public async Task<ActionResult<SuscripcionResponseDto>> Create([FromBody] SuscripcionCreateDto suscripcion)
    {
        var createdSuscripcion = await _suscripcionService.CreateAsync(suscripcion);
        return Created(string.Empty, createdSuscripcion);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SuscripcionUpdateDto suscripcion)
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
