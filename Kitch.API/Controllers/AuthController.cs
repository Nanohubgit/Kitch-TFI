using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Usuario>> Register([FromBody] Usuario usuario)
    {
        try
        {
            var createdUsuario = await _authService.RegisterAsync(usuario);
            return Created($"/api/usuarios/{createdUsuario.Id}", createdUsuario);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<Usuario>> Login([FromQuery] string email, [FromQuery] string contrasena)
    {
        var usuario = await _authService.LoginAsync(email, contrasena);

        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(usuario);
    }

    [HttpGet("email-existe")]
    public async Task<ActionResult<bool>> EmailExiste([FromQuery] string email)
    {
        var exists = await _authService.EmailExisteAsync(email);
        return Ok(exists);
    }
}
