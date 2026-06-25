using Kitch.Application.DTOs.Sustituciones;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SustitutosController : ApiControllerBase
{
    private readonly ISustitucionService _sustitucionService;

    public SustitutosController(ISustitucionService sustitucionService)
    {
        _sustitucionService = sustitucionService;
    }

    [HttpGet("ingrediente/{ingredienteId:int}")]
    public async Task<ActionResult<IEnumerable<SustitutoSugerido>>> BuscarSustitutos(int ingredienteId)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var sustitutos = await _sustitucionService.BuscarSustitutosAsync(usuarioId, ingredienteId);
        return Ok(sustitutos);
    }
}
