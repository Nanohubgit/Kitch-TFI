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
    /// Cascarón Frontend-Ready: inicia checkout y devuelve CheckoutUrl / PreferenceId para el modal de pago.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize(Roles = RolUsuario.Basico)]
    public async Task<ActionResult<CheckoutSuscripcionResponseDto>> Checkout(
        [FromBody] CheckoutSuscripcionRequestDto request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var result = await _suscripcionService.IniciarCheckoutAsync(usuarioId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequestMessage(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    /// <summary>
    /// Cascarón público: simula confirmación de la pasarela y asciende a Profesional.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<WebhookPagoResponseDto>> Webhook([FromBody] WebhookPagoRequestDto request)
    {
        try
        {
            var result = await _suscripcionService.ProcesarWebhookAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequestMessage(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    /// <summary>
    /// Contrata la suscripción Profesional: cobra vía pasarela y, si aprueba, actualiza el rol.
    /// </summary>
    [HttpPost("contratar")]
    [Authorize(Roles = RolUsuario.Basico)]
    public async Task<ActionResult<ContratarSuscripcionResult>> Contratar(
        [FromBody] ContratarSuscripcionRequest request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
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
            return BadRequestMessage(ex.Message);
        }
        catch (InvalidOperationException ex) when (EsUsuarioYaProfesional(ex))
        {
            return ForbiddenMessage(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
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
            return NotFound(new { message = "Suscripción no encontrada." });
        }

        return Ok(suscripcion);
    }

    [HttpPost]
    public async Task<ActionResult<SuscripcionResponseDto>> Create([FromBody] SuscripcionCreateDto suscripcion)
    {
        try
        {
            var createdSuscripcion = await _suscripcionService.CreateAsync(suscripcion);
            return Created(string.Empty, createdSuscripcion);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SuscripcionUpdateDto suscripcion)
    {
        try
        {
            var updated = await _suscripcionService.UpdateAsync(id, suscripcion);
            if (!updated)
            {
                return NotFound(new { message = "Suscripción no encontrada." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _suscripcionService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = "Suscripción no encontrada." });
        }

        return NoContent();
    }
}
