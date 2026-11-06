using Kitch.Application.DTOs.Recetas;
using Kitch.Application.Interfaces;
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
        var recetas = await _recetaService.GetAllAsync(GetUsuarioIdOrThrow());
        return Ok(recetas);
    }

    [HttpGet("categorias")]
    public ActionResult<IReadOnlyList<string>> GetCategorias()
    {
        return Ok(_recetaService.GetCategorias());
    }

    [HttpGet("categoria/{categoria}")]
    public async Task<ActionResult<IEnumerable<RecetaResponseDto>>> GetByCategoria(string categoria)
    {
        var recetas = await _recetaService.GetByCategoriaAsync(categoria, GetUsuarioIdOrThrow());
        return Ok(recetas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecetaResponseDto>> GetById(int id)
    {
        var receta = await _recetaService.GetByIdAsync(id, GetUsuarioIdOrThrow());
        if (receta is null)
        {
            throw new KeyNotFoundException("Receta no encontrada.");
        }

        return Ok(receta);
    }

    [HttpGet("dificultad/{dificultad}")]
    public async Task<ActionResult<IEnumerable<RecetaResponseDto>>> GetByDificultad(string dificultad)
    {
        var recetas = await _recetaService.GetByDificultadAsync(dificultad, GetUsuarioIdOrThrow());
        return Ok(recetas);
    }

    [HttpPost]
    public async Task<ActionResult<RecetaResponseDto>> Create([FromBody] RecetaCreateDto receta)
    {
        var createdReceta = await _recetaService.CreateAsync(receta, GetUsuarioIdOrThrow());
        return CreatedAtAction(nameof(GetById), new { id = createdReceta.Id }, createdReceta);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RecetaUpdateDto receta)
    {
        var updated = await _recetaService.UpdateAsync(id, receta, GetUsuarioIdOrThrow());
        if (!updated)
        {
            throw new KeyNotFoundException("Receta no encontrada.");
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _recetaService.DeleteAsync(id, GetUsuarioIdOrThrow());
        if (!deleted)
        {
            throw new KeyNotFoundException("Receta no encontrada.");
        }

        return NoContent();
    }
}
