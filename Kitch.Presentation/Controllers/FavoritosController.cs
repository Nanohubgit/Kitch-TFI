using Kitch.Application.DTOs.Favoritos;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
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

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<FavoritoResponseDto>>> GetByUsuarioId(int usuarioId)
    {
        if (!TryGetUsuarioId(out var usuarioActualId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        // Solo podés ver tus propios favoritos (salvo que seas Admin).
        if (usuarioActualId != usuarioId && !User.IsInRole(RolUsuario.Admin))
        {
            return Forbid();
        }

        var favoritos = await _favoritoService.GetByUsuarioIdAsync(usuarioId);
        return Ok(favoritos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FavoritoResponseDto>> GetById(int id)
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

    [HttpPost]
    public async Task<ActionResult<FavoritoResponseDto>> AddFavorito([FromBody] FavoritoCreateDto favorito)
    {
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
        var deleted = await _favoritoService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
