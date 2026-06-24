using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kitch.Application.DTOs.Auth;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kitch.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly KitchDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        KitchDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request is null)
        {
            throw new InvalidOperationException("Los datos de registro son obligatorios.");
        }

        var email = request.Email?.Trim() ?? string.Empty;
        var nombre = request.Nombre?.Trim() ?? string.Empty;
        var apellido = request.Apellido?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Nombre, apellido, email y contraseña son obligatorios.");
        }

        if (await EmailExisteAsync(email))
        {
            throw new InvalidOperationException("El email ya se encuentra registrado.");
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Activo = true,
            Rol = "Basico"
        };

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        return CreateAuthResponse(usuario);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (request is null)
        {
            throw new UnauthorizedAccessException("Email o contraseña inválidos.");
        }

        var email = request.Email?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new UnauthorizedAccessException("Email o contraseña inválidos.");
        }

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(usuario => usuario.Email == email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
        {
            throw new UnauthorizedAccessException("Email o contraseña inválidos.");
        }

        if (!usuario.Activo)
        {
            throw new UnauthorizedAccessException("El usuario se encuentra inactivo.");
        }

        return CreateAuthResponse(usuario);
    }

    public async Task<bool> EmailExisteAsync(string email)
    {
        var normalizedEmail = email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return false;
        }

        return await _context.Usuarios.AnyAsync(usuario => usuario.Email == normalizedEmail);
    }

    private AuthResponse CreateAuthResponse(Usuario usuario)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpiresInMinutes());
        var token = GenerateJwtToken(usuario, expiresAt);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Rol = usuario.Rol
        };
    }

    private string GenerateJwtToken(Usuario usuario, DateTime expiresAt)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetJwtExpiresInMinutes()
    {
        return int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var expiresInMinutes)
            ? expiresInMinutes
            : 60;
    }
}
