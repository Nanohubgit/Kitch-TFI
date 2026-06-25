using Kitch.Application.DTOs.Preparacion;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreparacionController : ControllerBase
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
}
