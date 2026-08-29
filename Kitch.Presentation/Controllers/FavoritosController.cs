using Kitch.Application.DTOs.Favoritos;
using Kitch.Application.Exceptions;
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
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var favoritos = await _favoritoService.GetByUsuarioIdAsync(usuarioId);
        return Ok(favoritos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FavoritoResponseDto>> GetById(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var favorito = await _favoritoService.GetByIdAsync(id, usuarioId);
        if (favorito is null)
        {
            return NotFound(new { message = "Favorito no encontrado." });
        }

        return Ok(favorito);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleFavorito([FromQuery] int recetaId)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var esFavorito = await _favoritoService.ToggleFavoritoAsync(usuarioId, recetaId);
            return Ok(new { esFavorito, message = esFavorito ? "Agregado a favoritos." : "Quitado de favoritos." });
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenMessage(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<FavoritoResponseDto>> AddFavorito([FromBody] FavoritoCreateDto favorito)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        favorito.UsuarioId = usuarioId;

        try
        {
            var createdFavorito = await _favoritoService.AddFavoritoAsync(favorito);
            return CreatedAtAction(nameof(GetById), new { id = createdFavorito.Id }, createdFavorito);
        }
        catch (ForbiddenException ex)
        {
            return ForbiddenMessage(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestMessage(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return UnauthorizedMessage("No se pudo identificar al usuario a partir del token.");
        }

        var deleted = await _favoritoService.DeleteAsync(id, usuarioId);
        if (!deleted)
        {
            return NotFound(new { message = "Favorito no encontrado." });
        }

        return NoContent();
    }
}
