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

    [HttpGet("mias")]
    public async Task<ActionResult<IEnumerable<ItemListaCompraResponseDto>>> GetMias()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var items = await _listaCompraService.GetByUsuarioIdAsync(usuarioId);
        return Ok(items);
    }

    [HttpGet("faltantes")]
    public async Task<ActionResult<IEnumerable<ItemListaCompraResponseDto>>> GetFaltantes()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var faltantes = await _listaCompraService.SincronizarFaltantesAsync(usuarioId);
        return Ok(faltantes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemListaCompraResponseDto>> GetById(int id)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var item = await _listaCompraService.GetByIdAsync(id, usuarioId);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemListaCompraResponseDto>> Create([FromBody] ItemListaCompraCreateDto item)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        item.UsuarioId = usuarioId;

        var createdItem = await _listaCompraService.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = createdItem.Id }, createdItem);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ItemListaCompraUpdateDto item)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var updated = await _listaCompraService.UpdateAsync(id, item, usuarioId);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("{id:int}/comprado")]
    public async Task<IActionResult> MarcarComoComprado(int id)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var updated = await _listaCompraService.MarcarComoCompradoAsync(id, usuarioId);

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
        var deleted = await _listaCompraService.DeleteAsync(id, usuarioId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
