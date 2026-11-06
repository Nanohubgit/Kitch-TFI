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
    public async Task<ActionResult<IEnumerable<FavoritoResponseDto>>> GetMias([FromQuery] string? categoria)
    {
        var favoritos = await _favoritoService.GetByUsuarioIdAsync(GetUsuarioIdOrThrow());
        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var clave = categoria.Trim().ToLowerInvariant();
            favoritos = favoritos.Where(favorito => favorito.RecetaCategoria == clave);
        }

        return Ok(favoritos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FavoritoResponseDto>> GetById(int id)
    {
        var favorito = await _favoritoService.GetByIdAsync(id, GetUsuarioIdOrThrow());
        if (favorito is null)
        {
            throw new KeyNotFoundException("Favorito no encontrado.");
        }

        return Ok(favorito);
    }

    [HttpPost("toggle")]
    public async Task<ActionResult<ToggleFavoritoResponseDto>> ToggleFavorito([FromQuery] int recetaId)
    {
        var esFavorito = await _favoritoService.ToggleFavoritoAsync(GetUsuarioIdOrThrow(), recetaId);
        return Ok(new ToggleFavoritoResponseDto
        {
            EsFavorito = esFavorito,
            Message = esFavorito ? "Agregado a favoritos." : "Quitado de favoritos."
        });
    }

    [HttpPost]
    public async Task<ActionResult<FavoritoResponseDto>> AddFavorito([FromBody] FavoritoCreateDto favorito)
    {
        favorito.UsuarioId = GetUsuarioIdOrThrow();
        var createdFavorito = await _favoritoService.AddFavoritoAsync(favorito);
        return CreatedAtAction(nameof(GetById), new { id = createdFavorito.Id }, createdFavorito);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _favoritoService.DeleteAsync(id, GetUsuarioIdOrThrow());
        if (!deleted)
        {
            throw new KeyNotFoundException("Favorito no encontrado.");
        }

        return NoContent();
    }
}
