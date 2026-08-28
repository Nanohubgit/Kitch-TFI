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

    [HttpGet("mias")]
    public async Task<ActionResult<IEnumerable<FavoritoResponseDto>>> GetMias()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var favoritos = await _favoritoService.GetByUsuarioIdAsync(usuarioId);
        return Ok(favoritos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FavoritoResponseDto>> GetById(int id)
    {
        var usuarioId = GetUsuarioIdOrThrow();
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
        var usuarioId = GetUsuarioIdOrThrow();
        var esFavorito = await _favoritoService.ToggleFavoritoAsync(usuarioId, recetaId);
        return Ok(new { esFavorito });
    }

    [HttpPost]
    public async Task<ActionResult<FavoritoResponseDto>> AddFavorito([FromBody] FavoritoCreateDto favorito)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        favorito.UsuarioId = usuarioId;

        var createdFavorito = await _favoritoService.AddFavoritoAsync(favorito);
        return CreatedAtAction(nameof(GetById), new { id = createdFavorito.Id }, createdFavorito);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var deleted = await _favoritoService.DeleteAsync(id, usuarioId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
