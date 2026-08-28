using Kitch.Application.DTOs.Sustitutos;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SustitutosController : ApiControllerBase
{
    private readonly ISustitutoService _sustitutoService;

    public SustitutosController(ISustitutoService sustitutoService)
    {
        _sustitutoService = sustitutoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SustitutoResponseDto>>> GetAll()
    {
        var sustitutos = await _sustitutoService.GetAllAsync();
        return Ok(sustitutos);
    }

    [HttpGet("ingrediente/{ingredienteId:int}")]
    public async Task<ActionResult<IEnumerable<SustitutoResponseDto>>> GetByIngredienteId(int ingredienteId)
    {
        GetUsuarioIdOrThrow();
        var sustitutos = await _sustitutoService.GetByIngredienteIdAsync(ingredienteId);
        return Ok(sustitutos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SustitutoResponseDto>> GetById(int id)
    {
        var sustituto = await _sustitutoService.GetByIdAsync(id);

        if (sustituto is null)
        {
            return NotFound();
        }

        return Ok(sustituto);
    }

    [HttpPost]
    public async Task<ActionResult<SustitutoResponseDto>> Create([FromBody] SustitutoCreateDto sustituto)
    {
        var createdSustituto = await _sustitutoService.CreateAsync(sustituto);
        return CreatedAtAction(nameof(GetById), new { id = createdSustituto.Id }, createdSustituto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SustitutoUpdateDto sustituto)
    {
        var updated = await _sustitutoService.UpdateAsync(id, sustituto);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _sustitutoService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
