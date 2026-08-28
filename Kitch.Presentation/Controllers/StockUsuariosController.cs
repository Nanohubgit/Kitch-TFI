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
        var usuarioId = GetUsuarioIdOrThrow();
        var stock = await _stockUsuarioService.GetByUsuarioIdAsync(usuarioId);
        return Ok(stock);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockUsuarioResponseDto>> GetById(int id)
    {
        var usuarioId = GetUsuarioIdOrThrow();
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
        var usuarioId = GetUsuarioIdOrThrow();
        stock.UsuarioId = usuarioId;

        var createdStock = await _stockUsuarioService.CreateAsync(stock);
        return CreatedAtAction(nameof(GetById), new { id = createdStock.Id }, createdStock);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StockUsuarioUpdateDto stock)
    {
        var usuarioId = GetUsuarioIdOrThrow();
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
        var usuarioId = GetUsuarioIdOrThrow();
        var deleted = await _stockUsuarioService.DeleteAsync(id, usuarioId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
