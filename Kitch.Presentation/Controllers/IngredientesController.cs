using Kitch.Application.DTOs.Ingredientes;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolUsuario.Admin)]
public class IngredientesController : ControllerBase
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
        try
        {
            var createdIngrediente = await _ingredienteService.CreateAsync(ingrediente);
            return Created(string.Empty, createdIngrediente);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] IngredienteUpdateDto ingrediente)
    {
        try
        {
            var updated = await _ingredienteService.UpdateAsync(id, ingrediente);

            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _ingredienteService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
