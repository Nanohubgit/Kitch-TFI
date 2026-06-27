using Kitch.Application.DTOs.Recetas;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecetasController : ControllerBase
{
    private readonly IRecetaService _recetaService;

    public RecetasController(IRecetaService recetaService)
    {
        _recetaService = recetaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecetaResponseDto>>> GetAll()
    {
        var recetas = await _recetaService.GetAllAsync();
        return Ok(recetas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecetaResponseDto>> GetById(int id)
    {
        var receta = await _recetaService.GetByIdAsync(id);

        if (receta is null)
        {
            return NotFound();
        }

        return Ok(receta);
    }

    [HttpGet("dificultad/{dificultad}")]
    public async Task<ActionResult<IEnumerable<RecetaResponseDto>>> GetByDificultad(DificultadReceta dificultad)
    {
        var recetas = await _recetaService.GetByDificultadAsync(dificultad);
        return Ok(recetas);
    }

    [HttpPost]
    public async Task<ActionResult<RecetaResponseDto>> Create([FromBody] RecetaCreateDto receta)
    {
        var createdReceta = await _recetaService.CreateAsync(receta);
        return Created(string.Empty, createdReceta);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RecetaUpdateDto receta)
    {
        var updated = await _recetaService.UpdateAsync(id, receta);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _recetaService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
