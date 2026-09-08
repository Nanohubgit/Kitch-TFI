using Kitch.Application.DTOs.Admin;
using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.DTOs.Usuarios;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ApiControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("metricas")]
    public async Task<ActionResult<MetricasPlataformaDto>> GetMetricas()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var metricas = await _adminService.GetMetricasAsync(usuarioId);
        return Ok(metricas);
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var usuarios = await _adminService.GetUsuariosAsync(usuarioId);
        return Ok(usuarios);
    }

    [HttpGet("suscripciones")]
    public async Task<ActionResult<IEnumerable<SuscripcionResponseDto>>> GetSuscripciones()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var suscripciones = await _adminService.GetSuscripcionesAsync(usuarioId);
        return Ok(suscripciones);
    }
}
