using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
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

    [HttpGet("mis")]
    public async Task<ActionResult<IEnumerable<ItemListaCompra>>> GetMisItems()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var items = await _listaCompraService.GetByUsuarioIdAsync(usuarioId);
        return Ok(items);
    }

    [HttpGet("faltantes")]
    public async Task<ActionResult<IEnumerable<ItemListaCompra>>> GetFaltantes()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var faltantes = await _listaCompraService.GenerarListaFaltantesAsync(usuarioId);
        return Ok(faltantes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemListaCompra>> GetById(int id)
    {
        var item = await _listaCompraService.GetByIdAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemListaCompra>> Create([FromBody] ItemListaCompra item)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        item.UsuarioId = usuarioId;

        var createdItem = await _listaCompraService.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = createdItem.Id }, createdItem);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ItemListaCompra item)
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
