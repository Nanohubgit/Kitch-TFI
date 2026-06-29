using Kitch.Application.DTOs.Preparacion;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreparacionController : ApiControllerBase
{
    private readonly IPreparacionService _preparacionService;

    public PreparacionController(IPreparacionService preparacionService)
    {
        _preparacionService = preparacionService;
    }

    [HttpPost("previsualizar-porciones")]
    public async Task<ActionResult<PrevisualizarPorcionesResponseDto>> PrevisualizarPorciones(
        [FromBody] PrevisualizarPorcionesRequestDto request)
    {
        try
        {
            var response = await _preparacionService.PrevisualizarRecalculoPorcionesAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("previsualizar-descuento-stock")]
    public async Task<ActionResult<PrevisualizarDescuentoStockResponseDto>> PrevisualizarDescuentoStock(
        [FromBody] PrevisualizarDescuentoStockRequestDto request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        // El stock que se previsualiza es siempre el del usuario logueado, no el que venga en el body.
        request.UsuarioId = usuarioId;

        try
        {
            var response = await _preparacionService.PrevisualizarDescuentoStockAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // "Finalizar y servir": descuenta de verdad los ingredientes de la alacena del usuario.
    [HttpPost("descontar-stock")]
    public async Task<IActionResult> DescontarStock([FromBody] ConfirmarDescuentoStockRequestDto request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            await _preparacionService.DescontarIngredientesAsync(usuarioId, request.RecetaId, request.PorcionesCocinadas);
            return Ok(new { mensaje = "Stock actualizado correctamente. ¡Buen provecho!" });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }
}
