using Kitch.Application.DTOs.Auth;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<Login2FaResponseDto>> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<Login2FaResponseDto>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<ActionResult<Verify2FaResponseDto>> Verify2Fa([FromBody] Verify2FaRequestDto request)
    {
        var response = await _authService.Verify2FaAsync(request);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<Verify2FaResponseDto>> Refresh()
    {
        var response = await _authService.RefrescarSesionAsync(GetUsuarioIdOrThrow());
        return Ok(response);
    }

    [HttpGet("perfil")]
    [Authorize]
    public async Task<ActionResult<PerfilUsuarioResponseDto>> GetPerfil()
    {
        var perfil = await _authService.GetPerfilAsync(GetUsuarioIdOrThrow());
        if (perfil is null)
        {
            throw new KeyNotFoundException("Usuario no encontrado.");
        }

        return Ok(perfil);
    }

    [HttpPut("perfil")]
    [Authorize]
    public async Task<ActionResult<PerfilUsuarioResponseDto>> EditarPerfil([FromBody] EditarPerfilRequestDto request)
    {
        var perfil = await _authService.EditarPerfilAsync(GetUsuarioIdOrThrow(), request);
        return Ok(perfil);
    }

    [HttpGet("email-existe")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> EmailExiste([FromQuery] string email)
    {
        var exists = await _authService.EmailExisteAsync(email);
        return Ok(exists);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<MensajeResponseDto>> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(new MensajeResponseDto
        {
            Message = "Si el correo está registrado, enviamos instrucciones para restablecer la contraseña."
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<MensajeResponseDto>> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new MensajeResponseDto
        {
            Message = "La contraseña se actualizó correctamente."
        });
    }
}
