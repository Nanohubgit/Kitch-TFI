using Kitch.Application.DTOs.Alacena;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlacenaController : ApiControllerBase
{
    private readonly IAlacenaService _alacenaService;

    public AlacenaController(IAlacenaService alacenaService)
    {
        _alacenaService = alacenaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IngredienteAlacenaResponseDto>>> ObtenerInventario()
    {
        var inventario = await _alacenaService.ObtenerInventarioAsync(GetUsuarioIdOrThrow());
        return Ok(inventario);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IngredienteAlacenaResponseDto>> ObtenerPorId(int id)
    {
        var item = await _alacenaService.ObtenerPorIdAsync(id, GetUsuarioIdOrThrow());
        if (item is null)
        {
            throw new KeyNotFoundException("Ingrediente no encontrado en la alacena.");
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<IngredienteAlacenaResponseDto>> AgregarIngrediente(
        [FromBody] AgregarIngredienteRequestDto request)
    {
        var creado = await _alacenaService.AgregarIngredienteAsync(GetUsuarioIdOrThrow(), request);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<IngredienteAlacenaResponseDto>> ActualizarCantidad(
        int id,
        [FromBody] ActualizarCantidadAlacenaRequestDto request)
    {
        var actualizado = await _alacenaService.ActualizarCantidadAsync(id, GetUsuarioIdOrThrow(), request);
        if (actualizado is null)
        {
            throw new KeyNotFoundException("Ingrediente no encontrado en la alacena.");
        }

        return Ok(actualizado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarIngrediente(int id)
    {
        var eliminado = await _alacenaService.EliminarIngredienteAsync(id, GetUsuarioIdOrThrow());
        if (!eliminado)
        {
            throw new KeyNotFoundException("Ingrediente no encontrado en la alacena.");
        }

        return NoContent();
    }
}
