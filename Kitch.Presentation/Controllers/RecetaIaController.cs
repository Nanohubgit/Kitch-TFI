using Kitch.Application.DTOs.RecetaIa;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecetaIaController : ApiControllerBase
{
    private readonly IRecetaIaService _recetaIaService;

    public RecetaIaController(IRecetaIaService recetaIaService)
    {
        _recetaIaService = recetaIaService;
    }

    [HttpPost("generar")]
    public async Task<ActionResult<RecetaGeneradaDto>> Generar([FromBody] GenerarRecetaRequest? request)
    {
        var receta = await _recetaIaService.GenerarRecetaAsync(
            GetUsuarioIdOrThrow(),
            request?.Preferencias);
        return Ok(receta);
    }

    [HttpPost("guardar")]
    public async Task<ActionResult<RecetaGuardadaResponse>> Guardar([FromBody] RecetaGeneradaDto receta)
    {
        var guardada = await _recetaIaService.GuardarRecetaAsync(GetUsuarioIdOrThrow(), receta);
        return Ok(guardada);
    }
}
