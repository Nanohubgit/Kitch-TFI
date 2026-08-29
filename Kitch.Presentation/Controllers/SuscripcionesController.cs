using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    /// Inicia Checkout Pro. Devuelve InitPoint. El rol NO se cambia acá.
    /// </summary>
    [HttpPost("contratar")]
    [Authorize(Roles = RolUsuario.Basico)]
    public async Task<ActionResult<IniciarPagoResponseDto>> Contratar(
        [FromBody] ContratarSuscripcionRequest? request)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var result = await _suscripcionService.ContratarAsync(usuarioId, request);
        return Ok(result);
    }

    /// <summary>
    /// Webhook público de Mercado Pago. Único camino que promueve a Profesional.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromQuery] string? type,
        [FromQuery] string? topic,
        [FromQuery] string? id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PasarelaWebhookRequest? body)
    {
        var paymentId = ExtraerPaymentId(type, topic, id, body);
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return Ok();
        }

        await _suscripcionService.ProcesarNotificacionPagoAsync(paymentId);
        return Ok();
    }

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
        return CreatedAtAction(nameof(GetById), new { id = createdSuscripcion.Id }, createdSuscripcion);
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

    private static string? ExtraerPaymentId(
        string? type,
        string? topic,
        string? id,
        PasarelaWebhookRequest? body)
    {
        var tipo = body?.Type ?? type ?? topic ?? body?.Topic;
        var esPago = string.IsNullOrWhiteSpace(tipo) ||
            tipo.Equals("payment", StringComparison.OrdinalIgnoreCase);

        if (!esPago)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(body?.Data?.Id))
        {
            return body.Data.Id.Trim();
        }

        return string.IsNullOrWhiteSpace(id) ? null : id.Trim();
    }
}
