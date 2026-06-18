using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritosController : ControllerBase
{
    private readonly IFavoritoService _favoritoService;

    public FavoritosController(IFavoritoService favoritoService)
    {
        _favoritoService = favoritoService;
    }

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<RecetaFavorita>>> GetByUsuarioId(int usuarioId)
    {
        var favoritos = await _favoritoService.GetByUsuarioIdAsync(usuarioId);
        return Ok(favoritos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecetaFavorita>> GetById(int id)
    {
        var favorito = await _favoritoService.GetByIdAsync(id);

        if (favorito is null)
        {
            return NotFound();
        }

        return Ok(favorito);
    }

    [HttpGet("existe")]
    public async Task<ActionResult<bool>> ExisteFavorito([FromQuery] int usuarioId, [FromQuery] int recetaId)
    {
        var exists = await _favoritoService.ExisteFavoritoAsync(usuarioId, recetaId);
        return Ok(exists);
    }

    [HttpPost]
    public async Task<ActionResult<RecetaFavorita>> AddFavorito([FromBody] RecetaFavorita favorito)
    {
        try
        {
            var createdFavorito = await _favoritoService.AddFavoritoAsync(favorito);
            return CreatedAtAction(nameof(GetById), new { id = createdFavorito.Id }, createdFavorito);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _favoritoService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

