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
        var response = await _preparacionService.PrevisualizarRecalculoPorcionesAsync(request);
        return Ok(response);
    }

    [HttpPost("previsualizar-descuento-stock")]
    public async Task<ActionResult<PrevisualizarDescuentoStockResponseDto>> PrevisualizarDescuentoStock(
        [FromBody] PrevisualizarDescuentoStockRequestDto request)
    {
        request.UsuarioId = GetUsuarioIdOrThrow();
        var response = await _preparacionService.PrevisualizarDescuentoStockAsync(request);
        return Ok(response);
    }

    [HttpPost("descontar-stock")]
    public async Task<IActionResult> DescontarStock([FromBody] ConfirmarDescuentoStockRequestDto request)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        await _preparacionService.DescontarIngredientesAsync(usuarioId, request.RecetaId, request.PorcionesCocinadas);
        return Ok(new { mensaje = "Stock actualizado correctamente. ¡Buen provecho!" });
    }
}
