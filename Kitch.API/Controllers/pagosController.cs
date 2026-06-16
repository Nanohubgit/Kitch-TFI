using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagosController : ControllerBase
{
    private readonly IPagoService _pagoService;

    public PagosController(IPagoService pagoService)
    {
        _pagoService = pagoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pago>>> GetAll()
    {
        var pagos = await _pagoService.GetAllAsync();
        return Ok(pagos);
    }

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<Pago>>> GetByUsuarioId(int usuarioId)
    {
        var pagos = await _pagoService.GetByUsuarioIdAsync(usuarioId);
        return Ok(pagos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Pago>> GetById(int id)
    {
        var pago = await _pagoService.GetByIdAsync(id);

        if (pago is null)
        {
            return NotFound();
        }

        return Ok(pago);
    }

    [HttpPost]
    public async Task<ActionResult<Pago>> Create([FromBody] Pago pago)
    {
        var createdPago = await _pagoService.CreateAsync(pago);
        return CreatedAtAction(nameof(GetById), new { id = createdPago.Id }, createdPago);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Pago pago)
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