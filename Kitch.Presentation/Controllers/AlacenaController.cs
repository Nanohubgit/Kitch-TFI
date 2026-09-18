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
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var inventario = await _alacenaService.ObtenerInventarioAsync(usuarioId);
        return Ok(inventario);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IngredienteAlacenaResponseDto>> ObtenerPorId(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var item = await _alacenaService.ObtenerPorIdAsync(id, usuarioId);
        if (item is null)
        {
            return NotFound(new { message = "Ingrediente no encontrado en la alacena." });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<IngredienteAlacenaResponseDto>> AgregarIngrediente(
        [FromBody] AgregarIngredienteRequestDto request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var creado = await _alacenaService.AgregarIngredienteAsync(usuarioId, request);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<IngredienteAlacenaResponseDto>> ActualizarCantidad(
        int id,
        [FromBody] ActualizarCantidadAlacenaRequestDto request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var actualizado = await _alacenaService.ActualizarCantidadAsync(id, usuarioId, request);
            if (actualizado is null)
            {
                return NotFound(new { message = "Ingrediente no encontrado en la alacena." });
            }

            return Ok(actualizado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarIngrediente(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var eliminado = await _alacenaService.EliminarIngredienteAsync(id, usuarioId);
        if (!eliminado)
        {
            return NotFound(new { message = "Ingrediente no encontrado en la alacena." });
        }

        return NoContent();
    }
}
