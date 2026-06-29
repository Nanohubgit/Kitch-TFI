using Kitch.Application.DTOs.Recomendacion;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecomendacionController : ApiControllerBase
{
    private readonly IRecomendacionService _recomendacionService;

    public RecomendacionController(IRecomendacionService recomendacionService)
    {
        _recomendacionService = recomendacionService;
    }

    // Recomendación determinística por % de coincidencia con la alacena.
    // Opcional: ?maxFaltantes=2 para el caso "te faltan pocos ingredientes".
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecetaCompatibleDto>>> Recomendar([FromQuery] int? maxFaltantes)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var recetas = await _recomendacionService.RecomendarAsync(usuarioId, maxFaltantes);
        return Ok(recetas);
    }
}
