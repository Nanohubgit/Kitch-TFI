using Kitch.Application.DTOs.StockUsuarios;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

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

    [HttpGet("mias")]
    public async Task<ActionResult<IEnumerable<StockUsuarioResponseDto>>> GetMias()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var stock = await _stockUsuarioService.GetByUsuarioIdAsync(usuarioId);
        return Ok(stock);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockUsuarioResponseDto>> GetById(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var stock = await _stockUsuarioService.GetByIdAsync(id, usuarioId);

        if (stock is null)
        {
            return NotFound();
        }

        return Ok(stock);
    }

    [HttpPost]
    public async Task<ActionResult<StockUsuarioResponseDto>> Create([FromBody] StockUsuarioCreateDto stock)
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
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StockUsuarioUpdateDto stock)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var updated = await _stockUsuarioService.UpdateAsync(id, stock, usuarioId);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var deleted = await _stockUsuarioService.DeleteAsync(id, usuarioId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
