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
    private readonly IPaymentGatewayService _paymentGateway;

    public SuscripcionesController(ISuscripcionService suscripcionService, IPaymentGatewayService paymentGateway)
    {
        _suscripcionService = suscripcionService;
        _paymentGateway = paymentGateway;
    }

    [HttpPost("contratar")]
    [Authorize(Roles = RolUsuario.Basico + "," + RolUsuario.Profesional)]
    public async Task<ActionResult<IniciarPagoResponseDto>> Contratar(
        [FromBody] ContratarSuscripcionRequest? request)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var result = await _suscripcionService.ContratarAsync(usuarioId, request);
        return Ok(result);
    }

    [HttpPost("confirmar-pago")]
    public async Task<ActionResult<EstadoPagoResponseDto>> ConfirmarPago([FromBody] ConfirmarPagoRequestDto request)
    {
        var resultado = await _suscripcionService.ConfirmarPagoAsync(GetUsuarioIdOrThrow(), request.PaymentId);
        return Ok(resultado);
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromQuery] string? type,
        [FromQuery] string? topic,
        [FromQuery] string? id,
        [FromQuery(Name = "data.id")] string? dataId,
        [FromHeader(Name = "x-signature")] string? signature,
        [FromHeader(Name = "x-request-id")] string? requestId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PasarelaWebhookRequest? body)
    {
        var paymentId = ExtraerPaymentId(type, topic, string.IsNullOrWhiteSpace(dataId) ? id : dataId, body);
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return Ok();
        }

        if (!_paymentGateway.FirmaWebhookValida(signature, requestId, paymentId))
        {
            throw new UnauthorizedAccessException("La notificación de Mercado Pago no es válida.");
        }

        await _suscripcionService.ProcesarNotificacionPagoAsync(paymentId);
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SuscripcionResponseDto>>> GetAll()
    {
        var adminId = GetUsuarioIdOrThrow();
        var suscripciones = await _suscripcionService.GetAllAsync(adminId);
        return Ok(suscripciones);
    }

    [HttpGet("mias")]
    public async Task<ActionResult<IEnumerable<SuscripcionResponseDto>>> GetMias()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var suscripciones = await _suscripcionService.GetByUsuarioIdAsync(usuarioId);
        return Ok(suscripciones);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SuscripcionResponseDto>> GetById(int id)
    {
        var solicitanteId = GetUsuarioIdOrThrow();
        var suscripcion = await _suscripcionService.GetByIdAsync(id, solicitanteId);
        if (suscripcion is null)
        {
            throw new KeyNotFoundException("Suscripción no encontrada.");
        }

        return Ok(suscripcion);
    }

    [HttpPost]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<ActionResult<SuscripcionResponseDto>> Create([FromBody] SuscripcionCreateDto suscripcion)
    {
        var createdSuscripcion = await _suscripcionService.CreateAsync(suscripcion, GetUsuarioIdOrThrow());
        return CreatedAtAction(nameof(GetById), new { id = createdSuscripcion.Id }, createdSuscripcion);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] SuscripcionUpdateDto suscripcion)
    {
        var updated = await _suscripcionService.UpdateAsync(id, suscripcion, GetUsuarioIdOrThrow());
        if (!updated)
        {
            throw new KeyNotFoundException("Suscripción no encontrada.");
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _suscripcionService.DeleteAsync(id, GetUsuarioIdOrThrow());
        if (!deleted)
        {
            throw new KeyNotFoundException("Suscripción no encontrada.");
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

        if (!string.IsNullOrWhiteSpace(id))
        {
            return id.Trim();
        }

        return string.IsNullOrWhiteSpace(body?.Data?.Id) ? null : body.Data.Id.Trim();
    }
}
