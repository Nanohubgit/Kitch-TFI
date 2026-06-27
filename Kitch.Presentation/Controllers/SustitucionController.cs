using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SustitucionController : ApiControllerBase
{
    private readonly ISustitucionService _sustitucionService;

    public SustitucionController(ISustitucionService sustitucionService)
    {
        _sustitucionService = sustitucionService;
    }

    // Asistente de sustitución inteligente: devuelve reemplazos viables priorizando
    // los que el usuario ya tiene en su alacena, con la equivalencia de medida.
    [HttpGet("ingrediente/{ingredienteId:int}")]
    public async Task<IActionResult> BuscarSustitutos(int ingredienteId)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        try
        {
            var sustitutos = await _sustitucionService.BuscarSustitutosAsync(usuarioId, ingredienteId);
            return Ok(sustitutos);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
