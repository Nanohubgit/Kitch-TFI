using Kitch.Application.DTOs.Admin;
using Kitch.Application.DTOs.Auth;
using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.DTOs.Usuarios;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolUsuario.Admin)]
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
        var metricas = await _adminService.GetMetricasAsync(GetUsuarioIdOrThrow());
        return Ok(metricas);
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios()
    {
        var usuarios = await _adminService.GetUsuariosAsync(GetUsuarioIdOrThrow());
        return Ok(usuarios);
    }

    [HttpGet("suscripciones")]
    public async Task<ActionResult<IEnumerable<SuscripcionResponseDto>>> GetSuscripciones()
    {
        var suscripciones = await _adminService.GetSuscripcionesAsync(GetUsuarioIdOrThrow());
        return Ok(suscripciones);
    }

    [HttpPost("usuarios/{id:int}/bloquear")]
    public async Task<ActionResult<UsuarioResponseDto>> Bloquear(int id)
    {
        var usuario = await _adminService.CambiarBloqueoAsync(GetUsuarioIdOrThrow(), id, bloquear: true);
        return Ok(usuario);
    }

    [HttpPost("usuarios/{id:int}/desbloquear")]
    public async Task<ActionResult<UsuarioResponseDto>> Desbloquear(int id)
    {
        var usuario = await _adminService.CambiarBloqueoAsync(GetUsuarioIdOrThrow(), id, bloquear: false);
        return Ok(usuario);
    }

    [HttpPost("usuarios/{id:int}/recuperar-password")]
    public async Task<ActionResult<MensajeResponseDto>> RecuperarPassword(int id)
    {
        await _adminService.EnviarRecuperacionPasswordAsync(GetUsuarioIdOrThrow(), id);
        return Ok(new MensajeResponseDto
        {
            Message = "Enviamos al usuario un correo con el enlace para restablecer su contraseña."
        });
    }
}
