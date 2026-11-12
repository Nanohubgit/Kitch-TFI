using Kitch.Application.DTOs.ListaCompra;
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
        request.UsuarioId = GetUsuarioIdOrThrow();
        var response = await _preparacionService.PrevisualizarRecalculoPorcionesAsync(request);
        return Ok(response);
    }

    [HttpPost("agregar-faltantes")]
    public async Task<ActionResult<IEnumerable<ItemListaCompraResponseDto>>> AgregarFaltantes(
        [FromBody] AgregarFaltantesPorcionesRequestDto request)
    {
        var items = await _preparacionService.AgregarFaltantesPorcionesAsync(
            GetUsuarioIdOrThrow(),
            request.RecetaId,
            request.NuevasPorciones,
            request.Sustituciones);
        return Ok(items);
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
    public async Task<ActionResult<DescuentoStockResultadoDto>> DescontarStock(
        [FromBody] ConfirmarDescuentoStockRequestDto request)
    {
        var resultado = await _preparacionService.DescontarIngredientesAsync(
            GetUsuarioIdOrThrow(),
            request.RecetaId,
            request.PorcionesCocinadas,
            request.CantidadesUtilizadas,
            request.Sustituciones);
        return Ok(resultado);
    }

    [HttpPost("descontar-stock-parcial")]
    public async Task<ActionResult<DescuentoStockResultadoDto>> DescontarStockParcial(
        [FromBody] ConfirmarDescuentoStockRequestDto request)
    {
        var resultado = await _preparacionService.DescontarIngredientesParcialAsync(
            GetUsuarioIdOrThrow(),
            request.RecetaId,
            request.PorcionesCocinadas,
            request.Sustituciones);
        return Ok(resultado);
    }

    [HttpPost("aplicar-sustituto")]
    public async Task<ActionResult<AplicarSustitutoResponseDto>> AplicarSustituto(
        [FromBody] AplicarSustitutoRequestDto request)
    {
        var resultado = await _preparacionService.AplicarSustitutoAsync(GetUsuarioIdOrThrow(), request);
        return Ok(resultado);
    }
}
