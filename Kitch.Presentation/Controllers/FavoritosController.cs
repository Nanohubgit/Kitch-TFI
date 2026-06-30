using Kitch.Application.DTOs.Favoritos;
using Kitch.Application.Interfaces;
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

<<<<<<< HEAD
    // Tus propios favoritos. El usuario sale del token; no se pasa id ni email por la URL.
    [HttpGet]
=======
>>>>>>> main
    [HttpGet("mias")]
    public async Task<ActionResult<IEnumerable<FavoritoResponseDto>>> GetMias()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var favoritos = await _favoritoService.GetByUsuarioIdAsync(usuarioId);
        return Ok(favoritos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FavoritoResponseDto>> GetById(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var favorito = await _favoritoService.GetByIdAsync(id, usuarioId);

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

    [HttpPost]
    public async Task<ActionResult<FavoritoResponseDto>> AddFavorito([FromBody] FavoritoCreateDto favorito)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        favorito.UsuarioId = usuarioId;

        try
        {
            var createdFavorito = await _favoritoService.AddFavoritoAsync(favorito);
            return Created(string.Empty, createdFavorito);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var deleted = await _favoritoService.DeleteAsync(id, usuarioId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
