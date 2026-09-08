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
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Primer factor. Nunca emite JWT: envía el código 2FA al correo.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<Login2FaResponseDto>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Segundo factor: valida el código de 6 dígitos y emite el JWT definitivo.
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<ActionResult<Verify2FaResponseDto>> Verify2Fa([FromBody] Verify2FaRequestDto request)
    {
        try
        {
            var response = await _authService.Verify2FaAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Perfil del usuario autenticado (panel / navbar). Requiere Bearer JWT.
    /// </summary>
    [HttpGet("perfil")]
    [Authorize]
    public async Task<ActionResult<PerfilUsuarioResponseDto>> GetPerfil()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario a partir del token." });
        }

        var perfil = await _authService.GetPerfilAsync(usuarioId);
        if (perfil is null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        return Ok(perfil);
    }

    /// <summary>
    /// Actualiza NombreUsuario y PreferenciaDietetica. Requiere Bearer JWT.
    /// </summary>
    [HttpPut("perfil")]
    [Authorize]
    public async Task<ActionResult<PerfilUsuarioResponseDto>> EditarPerfil([FromBody] EditarPerfilRequestDto request)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario a partir del token." });
        }

        try
        {
            var perfil = await _authService.EditarPerfilAsync(usuarioId, request);
            return Ok(perfil);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("email-existe")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> EmailExiste([FromQuery] string email)
    {
        var exists = await _authService.EmailExisteAsync(email);
        return Ok(exists);
    }

    /// <summary>
    /// Inicia la recuperación de contraseña. Siempre responde 200 OK con mensaje genérico
    /// (no revela si el email está registrado).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        await _authService.ForgotPasswordAsync(request);

        return Ok(new
        {
            message = "Si el correo está registrado, enviamos instrucciones para restablecer la contraseña."
        });
    }

    /// <summary>
    /// Completa el restablecimiento con el token del email y la nueva contraseña.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "La contraseña se actualizó correctamente." });
    }
}
