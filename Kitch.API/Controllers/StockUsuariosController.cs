using Kitch.Application.DTOs.StockUsuarios;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockUsuariosController : ControllerBase
{
    private readonly IStockUsuarioService _stockUsuarioService;

    public StockUsuariosController(IStockUsuarioService stockUsuarioService)
    {
        _stockUsuarioService = stockUsuarioService;
    }

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<StockUsuarioResponseDto>>> GetByUsuarioId(int usuarioId)
    {
        var stock = await _stockUsuarioService.GetByUsuarioIdAsync(usuarioId);
        return Ok(stock);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockUsuarioResponseDto>> GetById(int id)
    {
        var stock = await _stockUsuarioService.GetByIdAsync(id);

        if (stock is null)
        {
            return NotFound();
        }

        return Ok(stock);
    }

    [HttpPost]
    public async Task<ActionResult<StockUsuarioResponseDto>> Create([FromBody] StockUsuarioCreateDto stock)
    {
        var createdStock = await _stockUsuarioService.CreateAsync(stock);
        return Created(string.Empty, createdStock);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StockUsuarioUpdateDto stock)
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
