using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritosController : ApiControllerBase
{
    private readonly IFavoritoService _favoritoService;

    public FavoritosController(IFavoritoService favoritoService)
    {
        _favoritoService = favoritoService;
    }

    [HttpGet("mis")]
    public async Task<ActionResult<IEnumerable<RecetaFavorita>>> GetMisFavoritos()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

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

    [HttpPost("toggle")]
    public async Task<ActionResult<object>> ToggleFavorito([FromQuery] int recetaId)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var esFavorito = await _favoritoService.ToggleFavoritoAsync(usuarioId, recetaId);
        return Ok(new { esFavorito });
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
