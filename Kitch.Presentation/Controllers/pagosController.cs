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
        var pagos = await _pagoService.GetAllAsync();
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
        var pago = await _pagoService.GetByIdAsync(id);

        if (pago is null)
        {
            return NotFound();
        }

        return Ok(pago);
    }

    [HttpPost]
    public async Task<ActionResult<PagoResponseDto>> Create([FromBody] PagoCreateDto pago)
    {
        var createdPago = await _pagoService.CreateAsync(pago);
        return CreatedAtAction(nameof(GetById), new { id = createdPago.Id }, createdPago);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PagoUpdateDto pago)
    {
        var updated = await _pagoService.UpdateAsync(id, pago);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _pagoService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
