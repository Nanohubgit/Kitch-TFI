using Kitch.Application.DTOs.ListaCompra;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ListaCompraController : ApiControllerBase
{
    private readonly IListaCompraService _listaCompraService;

    public ListaCompraController(IListaCompraService listaCompraService)
    {
        _listaCompraService = listaCompraService;
    }

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<ItemListaCompraResponseDto>>> GetByUsuarioId(int usuarioId)
    {
        if (!TryGetUsuarioId(out var usuarioActualId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var items = await _listaCompraService.GetByUsuarioIdAsync(usuarioId);
        return Ok(items);
    }

    [HttpGet("faltantes")]
    public async Task<ActionResult<IEnumerable<ItemListaCompraResponseDto>>> GetFaltantes()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var faltantes = await _listaCompraService.GenerarListaFaltantesAsync(usuarioId);
        return Ok(faltantes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemListaCompraResponseDto>> GetById(int id)
    {
        var item = await _listaCompraService.GetByIdAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemListaCompraResponseDto>> Create([FromBody] ItemListaCompraCreateDto item)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        item.UsuarioId = usuarioId;

        var createdItem = await _listaCompraService.CreateAsync(item);
        return Created(string.Empty, createdItem);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ItemListaCompraUpdateDto item)
    {
        var updated = await _listaCompraService.UpdateAsync(id, item);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("{id:int}/comprado")]
    public async Task<IActionResult> MarcarComoComprado(int id)
    {
        var updated = await _listaCompraService.MarcarComoCompradoAsync(id);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _listaCompraService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
