using Kitch.Application.DTOs.Ingredientes;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IngredientesController : ApiControllerBase
{
    private readonly IIngredienteService _ingredienteService;

    public IngredientesController(IIngredienteService ingredienteService)
    {
        _ingredienteService = ingredienteService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IngredienteResponseDto>>> GetAll()
    {
        var ingredientes = await _ingredienteService.GetAllAsync();
        return Ok(ingredientes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IngredienteResponseDto>> GetById(int id)
    {
        var ingrediente = await _ingredienteService.GetByIdAsync(id);

        if (ingrediente is null)
        {
            return NotFound();
        }

        return Ok(ingrediente);
    }

    [HttpPost]
    public async Task<ActionResult<IngredienteResponseDto>> Create([FromBody] IngredienteCreateDto ingrediente)
    {
        var adminId = GetUsuarioIdOrThrow();
        var createdIngrediente = await _ingredienteService.CreateAsync(ingrediente, adminId);
        return CreatedAtAction(nameof(GetById), new { id = createdIngrediente.Id }, createdIngrediente);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] IngredienteUpdateDto ingrediente)
    {
        var adminId = GetUsuarioIdOrThrow();
        var updated = await _ingredienteService.UpdateAsync(id, ingrediente, adminId);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var adminId = GetUsuarioIdOrThrow();
        var deleted = await _ingredienteService.DeleteAsync(id, adminId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
