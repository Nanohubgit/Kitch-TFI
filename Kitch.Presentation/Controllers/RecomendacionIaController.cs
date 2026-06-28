using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecomendacionIaController : ApiControllerBase
{
    private readonly IRecomendacionIaService _recomendacionIaService;

    public RecomendacionIaController(IRecomendacionIaService recomendacionIaService)
    {
        _recomendacionIaService = recomendacionIaService;
    }

    [HttpPost("recetas")]
    public async Task<IActionResult> RecomendarRecetas([FromBody] RecomendacionRequestDto? request)
    {
        // El usuario sale del token JWT, no del body: nadie puede pedir recomendaciones de otro.
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var recomendacion = await _recomendacionIaService.RecomendarRecetasAsync(usuarioId, request?.Preferencias);
        return Ok(new { Recomendacion = recomendacion });
    }
}

public class RecomendacionRequestDto
{
    public string? Preferencias { get; set; }
}
