using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockUsuariosController : ApiControllerBase
{
    private readonly IStockUsuarioService _stockUsuarioService;

    public StockUsuariosController(IStockUsuarioService stockUsuarioService)
    {
        _stockUsuarioService = stockUsuarioService;
    }

    [HttpGet("mis")]
    public async Task<ActionResult<IEnumerable<StockUsuario>>> GetMisStock()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var stock = await _stockUsuarioService.GetByUsuarioIdAsync(usuarioId);
        return Ok(stock);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockUsuario>> GetById(int id)
    {
        var stock = await _stockUsuarioService.GetByIdAsync(id);

        if (stock is null)
        {
            return NotFound();
        }

        return Ok(stock);
    }

    [HttpPost]
    public async Task<ActionResult<StockUsuario>> Create([FromBody] StockUsuario stock)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        stock.UsuarioId = usuarioId;

        try
        {
            var createdStock = await _stockUsuarioService.CreateAsync(stock);
            return CreatedAtAction(nameof(GetById), new { id = createdStock.Id }, createdStock);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StockUsuario stock)
    {
        var updated = await _stockUsuarioService.UpdateAsync(id, stock);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _stockUsuarioService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
