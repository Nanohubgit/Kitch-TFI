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
        var items = await _listaCompraService.GetByUsuarioIdAsync(GetUsuarioIdOrThrow());
        return Ok(items);
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ListaCompraResumenDto>> GetResumen()
    {
        var resumen = await _listaCompraService.GetResumenAsync(GetUsuarioIdOrThrow());
        return Ok(resumen);
    }

    [HttpGet("faltantes")]
    public async Task<ActionResult<IEnumerable<ItemListaCompraResponseDto>>> GetFaltantes()
    {
        var faltantes = await _listaCompraService.SincronizarFaltantesAsync(GetUsuarioIdOrThrow());
        return Ok(faltantes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemListaCompraResponseDto>> GetById(int id)
    {
        var item = await _listaCompraService.GetByIdAsync(id, GetUsuarioIdOrThrow());
        if (item is null)
        {
            throw new KeyNotFoundException("Ítem de lista de compras no encontrado.");
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemListaCompraResponseDto>> Create([FromBody] ItemListaCompraCreateDto item)
    {
        item.UsuarioId = GetUsuarioIdOrThrow();
        var createdItem = await _listaCompraService.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = createdItem.Id }, createdItem);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ItemListaCompraUpdateDto item)
    {
        var updated = await _listaCompraService.UpdateAsync(id, item, GetUsuarioIdOrThrow());
        if (!updated)
        {
            throw new KeyNotFoundException("Ítem de lista de compras no encontrado.");
        }

        return NoContent();
    }

    [HttpPatch("{id:int}/comprado")]
    public async Task<IActionResult> MarcarComoComprado(int id)
    {
        var updated = await _listaCompraService.MarcarComoCompradoAsync(id, GetUsuarioIdOrThrow());
        if (!updated)
        {
            throw new KeyNotFoundException("Ítem de lista de compras no encontrado.");
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _listaCompraService.DeleteAsync(id, GetUsuarioIdOrThrow());
        if (!deleted)
        {
            throw new KeyNotFoundException("Ítem de lista de compras no encontrado.");
        }

        return NoContent();
    }
}
