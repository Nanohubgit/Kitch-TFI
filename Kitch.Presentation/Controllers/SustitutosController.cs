using Kitch.Application.DTOs.Sustituciones;
using Kitch.Application.DTOs.Sustitutos;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SustitutosController : ApiControllerBase
{
    private readonly ISustitutoService _sustitutoService;
    private readonly ISustitucionService _sustitucionService;
    private readonly IIngredienteService _ingredienteService;

    public SustitutosController(
        ISustitutoService sustitutoService,
        ISustitucionService sustitucionService,
        IIngredienteService ingredienteService)
    {
        _sustitutoService = sustitutoService;
        _sustitucionService = sustitucionService;
        _ingredienteService = ingredienteService;
    }

    [HttpGet]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<ActionResult<IEnumerable<SustitutoResponseDto>>> GetAll()
    {
        var sustitutos = await _sustitutoService.GetAllAsync();
        return Ok(sustitutos);
    }

    [HttpGet("sugeridos/{ingredienteId:int}")]
    public async Task<ActionResult<IEnumerable<SustitutoSugerido>>> Sugeridos(int ingredienteId)
    {
        await AsegurarIngredienteAccesibleAsync(ingredienteId);
        var sugeridos = await _sustitucionService.BuscarSustitutosAsync(GetUsuarioIdOrThrow(), ingredienteId);
        return Ok(sugeridos);
    }

    [HttpGet("ingrediente/{ingredienteId:int}")]
    public async Task<ActionResult<IEnumerable<SustitutoResponseDto>>> GetByIngredienteId(int ingredienteId)
    {
        await AsegurarIngredienteAccesibleAsync(ingredienteId);
        var sustitutos = await _sustitutoService.GetByIngredienteIdAsync(ingredienteId, GetUsuarioIdOrThrow());
        return Ok(sustitutos);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<ActionResult<SustitutoResponseDto>> GetById(int id)
    {
        var sustituto = await _sustitutoService.GetByIdAsync(id);
        if (sustituto is null)
        {
            throw new KeyNotFoundException("Sustituto no encontrado.");
        }

        return Ok(sustituto);
    }

    [HttpPost]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<ActionResult<SustitutoResponseDto>> Create([FromBody] SustitutoCreateDto sustituto)
    {
        var createdSustituto = await _sustitutoService.CreateAsync(sustituto);
        return CreatedAtAction(nameof(GetById), new { id = createdSustituto.Id }, createdSustituto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] SustitutoUpdateDto sustituto)
    {
        var updated = await _sustitutoService.UpdateAsync(id, sustituto);
        if (!updated)
        {
            throw new KeyNotFoundException("Sustituto no encontrado.");
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolUsuario.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _sustitutoService.DeleteAsync(id);
        if (!deleted)
        {
            throw new KeyNotFoundException("Sustituto no encontrado.");
        }

        return NoContent();
    }

    private async Task AsegurarIngredienteAccesibleAsync(int ingredienteId)
    {
        if (User.IsInRole(RolUsuario.Admin))
        {
            return;
        }

        var accesibles = await _ingredienteService.ObtenerIdsAccesiblesAsync(GetUsuarioIdOrThrow());
        if (!accesibles.Contains(ingredienteId))
        {
            throw new KeyNotFoundException("El ingrediente no está en tu alacena, tus recetas ni tu lista de compras.");
        }
    }
}
