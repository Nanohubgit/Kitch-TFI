using Kitch.Application.DTOs.Pagos;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagosController : ApiControllerBase
{
    private readonly IPagoService _pagoService;

    public PagosController(IPagoService pagoService)
    {
        _pagoService = pagoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PagoResponseDto>>> GetAll()
    {
        var adminId = GetUsuarioIdOrThrow();
        var pagos = await _pagoService.GetAllAsync(adminId);
        return Ok(pagos);
    }

    [HttpGet("mios")]
    public async Task<ActionResult<IEnumerable<PagoResponseDto>>> GetMios()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var pagos = await _pagoService.GetByUsuarioIdAsync(usuarioId);
        return Ok(pagos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PagoResponseDto>> GetById(int id)
    {
        var solicitanteId = GetUsuarioIdOrThrow();
        var pago = await _pagoService.GetByIdAsync(id, solicitanteId);

        if (pago is null)
        {
            return NotFound();
        }

        return Ok(pago);
    }

    /// <summary>Solo administrador. El usuario no puede crear pagos de su historial.</summary>
    [HttpPost]
    public async Task<ActionResult<PagoResponseDto>> Create([FromBody] PagoCreateDto pago)
    {
        var adminId = GetUsuarioIdOrThrow();
        var createdPago = await _pagoService.CreateAsync(pago, adminId);
        return CreatedAtAction(nameof(GetById), new { id = createdPago.Id }, createdPago);
    }

    /// <summary>Solo administrador. El historial del usuario es de solo lectura.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PagoUpdateDto pago)
    {
        var adminId = GetUsuarioIdOrThrow();
        var updated = await _pagoService.UpdateAsync(id, pago, adminId);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>Solo administrador. El usuario no puede borrar su historial de pagos.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var adminId = GetUsuarioIdOrThrow();
        var deleted = await _pagoService.DeleteAsync(id, adminId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
