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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecetaCompatibleDto>>> Recomendar([FromQuery] int? maxFaltantes)
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var recetas = await _recomendacionService.RecomendarAsync(usuarioId, maxFaltantes);
        return Ok(recetas);
    }
}
