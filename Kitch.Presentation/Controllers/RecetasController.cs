using Kitch.Application.DTOs.Recetas;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecetasController : ApiControllerBase
{
    private readonly IRecetaService _recetaService;

    public RecetasController(IRecetaService recetaService)
    {
        _recetaService = recetaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecetaResponseDto>>> GetAll()
    {
        var recetas = await _recetaService.GetAllAsync(GetRolOrNull());
        return Ok(recetas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecetaResponseDto>> GetById(int id)
    {
        try
        {
            var receta = await _recetaService.GetByIdAsync(id, GetRolOrNull());
            if (receta is null)
            {
                return NotFound(new { message = "Receta no encontrada." });
            }

            return Ok(receta);
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenMessage(ex.Message);
        }
    }

    [HttpGet("dificultad/{dificultad}")]
    public async Task<ActionResult<IEnumerable<RecetaResponseDto>>> GetByDificultad(DificultadReceta dificultad)
    {
        try
        {
            var recetas = await _recetaService.GetByDificultadAsync(dificultad, GetRolOrNull());
            return Ok(recetas);
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenMessage(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<RecetaResponseDto>> Create([FromBody] RecetaCreateDto receta)
    {
        try
        {
            var createdReceta = await _recetaService.CreateAsync(receta);
            return CreatedAtAction(nameof(GetById), new { id = createdReceta.Id }, createdReceta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RecetaUpdateDto receta)
    {
        try
        {
            var updated = await _recetaService.UpdateAsync(id, receta);
            if (!updated)
            {
                return NotFound(new { message = "Receta no encontrada." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var adminId = GetUsuarioIdOrThrow();
        var deleted = await _recetaService.DeleteAsync(id, adminId);
        if (!deleted)
        {
            return NotFound(new { message = "Receta no encontrada." });
        }

        return NoContent();
    }
}
