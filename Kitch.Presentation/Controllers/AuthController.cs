using Kitch.Application.DTOs.Auth;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Primer factor. Nunca emite JWT: envía el código 2FA al correo.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<Login2FaResponseDto>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Segundo factor. Aquí y solo aquí se emite el JWT de sesión.
    /// </summary>
    [HttpPost("verify-2fa")]
    public async Task<ActionResult<AuthResponse>> VerifyTwoFactor([FromBody] Verify2FaRequestDto request)
    {
        var response = await _authService.VerifyTwoFactorAsync(request);
        return Ok(response);
    }

    [HttpGet("email-existe")]
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
