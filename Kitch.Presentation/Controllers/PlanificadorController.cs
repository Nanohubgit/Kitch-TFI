using Kitch.Application.DTOs.Planificador;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanificadorController : ApiControllerBase
{
    private readonly IPlanificadorService _planificadorService;

    public PlanificadorController(IPlanificadorService planificadorService)
    {
        _planificadorService = planificadorService;
    }

    [HttpGet("mias")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetMias()
    {
        var comidas = await _planificadorService.GetByUsuarioIdAsync(GetUsuarioIdOrThrow());
        return Ok(comidas);
    }

    [HttpGet("mias/fecha")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetMiasPorFecha(
        [FromQuery] DateTime fecha)
    {
        var comidas = await _planificadorService.GetByFechaAsync(GetUsuarioIdOrThrow(), fecha);
        return Ok(comidas);
    }

    [HttpGet("mias/rango")]
    public async Task<ActionResult<IEnumerable<ComidaPlanificadaResponseDto>>> GetMiasPorRango(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta)
    {
        var comidas = await _planificadorService.GetByRangoFechasAsync(GetUsuarioIdOrThrow(), desde, hasta);
        return Ok(comidas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> GetById(int id)
    {
        var comida = await _planificadorService.GetByIdAsync(id, GetUsuarioIdOrThrow());
        if (comida is null)
        {
            throw new KeyNotFoundException("Comida planificada no encontrada.");
        }

        return Ok(comida);
    }

    [HttpPost]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> Create([FromBody] ComidaPlanificadaCreateDto comida)
    {
        comida.UsuarioId = GetUsuarioIdOrThrow();
        var createdComida = await _planificadorService.CreateAsync(comida);
        return CreatedAtAction(nameof(GetById), new { id = createdComida.Id }, createdComida);
    }

    [HttpPost("agendar")]
    public async Task<ActionResult<ComidaPlanificadaResponseDto>> Agendar([FromBody] AgendarRecetaRequestDto request)
    {
        var createdComida = await _planificadorService.AgendarAsync(GetUsuarioIdOrThrow(), request);
        return CreatedAtAction(nameof(GetById), new { id = createdComida.Id }, createdComida);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ComidaPlanificadaUpdateDto comida)
    {
        var updated = await _planificadorService.UpdateAsync(id, comida, GetUsuarioIdOrThrow());
        if (!updated)
        {
            throw new KeyNotFoundException("Comida planificada no encontrada.");
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _planificadorService.DeleteAsync(id, GetUsuarioIdOrThrow());
        if (!deleted)
        {
            throw new KeyNotFoundException("Comida planificada no encontrada.");
        }

        return NoContent();
    }
}
