using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kitch.Application.DTOs.Auth;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kitch.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const int PasswordResetTokenBytes = 32;
    private const int PasswordResetExpirationMinutes = 30;

    private readonly KitchDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthService(
        KitchDbContext context,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
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
        var nombreUsuario = request.NombreUsuario?.Trim() ?? string.Empty;
        var preferenciaDietetica = string.IsNullOrWhiteSpace(request.PreferenciaDietetica)
            ? "Ninguna"
            : request.PreferenciaDietetica.Trim();
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(nombreUsuario) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Nombre, apellido, nombre de usuario, email y contraseña son obligatorios.");
        }

        if (await EmailExisteAsync(email))
        {
            throw new InvalidOperationException("El email ya se encuentra registrado.");
        }

        // Unicidad de handle: el índice único en DB es red de seguridad; este chequeo da error de negocio claro.
        if (await NombreUsuarioExisteAsync(nombreUsuario))
        {
            throw new InvalidOperationException("El nombre de usuario ya está en uso.");
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Apellido = apellido,
            NombreUsuario = nombreUsuario,
            Email = email,
            PreferenciaDietetica = preferenciaDietetica,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Activo = true,
            Rol = RolUsuario.Basico
        };

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        return CreateAuthResponse(usuario);
    }

    private async Task<bool> NombreUsuarioExisteAsync(string nombreUsuario)
    {
        var normalized = nombreUsuario?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return await _context.Usuarios.AnyAsync(u => u.NombreUsuario == normalized);
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

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        if (request is null)
        {
            return;
        }

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            // Respuesta genérica: no revelamos si el email existe o el formato falló "por seguridad de enumeración".
            return;
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        // Si no hay usuario (o está inactivo), salimos en silencio: misma "forma" de respuesta al exterior.
        if (usuario is null)
        {
            return;
        }

        // 1) Token plano: 32 bytes de CSPRNG (256 bits). Imposible de adivinar por fuerza bruta práctica.
        var tokenBytes = RandomNumberGenerator.GetBytes(PasswordResetTokenBytes);

        // 2) Codificación Base64Url: apta para URLs/email sin '+' '/' '=' que rompen links.
        var tokenPlano = ToBase64Url(tokenBytes);

        // 3) En DB solo el SHA-256 del token (hex). Si filtran la DB, no obtienen el token del mail.
        usuario.PasswordResetTokenHash = HashTokenSha256(tokenPlano);
        usuario.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetExpirationMinutes);

        await _context.SaveChangesAsync();

        // 4) El token PLANo solo viaja por email (canal que controla el dueño de la casilla).
        var body =
            "Recibimos una solicitud para restablecer tu contraseña en Alacena Virtual / Kitch.\n\n" +
            $"Usá este token (válido {PasswordResetExpirationMinutes} minutos):\n\n" +
            $"{tokenPlano}\n\n" +
            "Si no pediste este cambio, ignorá este correo.";

        await _emailService.SendEmailAsync(
            to: usuario.Email,
            subject: "Restablecé tu contraseña — Alacena Virtual",
            body: body);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        if (request is null)
        {
            throw new InvalidOperationException("Los datos de restablecimiento son obligatorios.");
        }

        var tokenPlano = request.Token?.Trim() ?? string.Empty;
        var nuevaPassword = request.NuevaPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(tokenPlano) || string.IsNullOrWhiteSpace(nuevaPassword))
        {
            throw new InvalidOperationException("Token y nueva contraseña son obligatorios.");
        }

        if (nuevaPassword.Length < 6)
        {
            throw new InvalidOperationException("La nueva contraseña debe tener al menos 6 caracteres.");
        }

        // Mismo algoritmo que al guardar: hashear lo que llegó del cliente y buscar por huella.
        var tokenHash = HashTokenSha256(tokenPlano);

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash);

        // Mensaje único: no distinguir "token inexistente" vs "expirado" (menos superficie de ataque).
        if (usuario is null ||
            usuario.PasswordResetTokenExpiresAt is null ||
            usuario.PasswordResetTokenExpiresAt <= DateTime.UtcNow ||
            !usuario.Activo)
        {
            throw new InvalidOperationException("Token inválido o expirado.");
        }

        // Misma lógica de Register: BCrypt del password humano (con sal), no SHA-256.
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nuevaPassword);

        // Un solo uso: invalidar el token inmediatamente tras el cambio.
        usuario.PasswordResetTokenHash = null;
        usuario.PasswordResetTokenExpiresAt = null;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// SHA-256 del token en texto → hex minúsculas.
    /// Ideal para tokens de alta entropía: barato de calcular y no reversible en la práctica.
    /// No usamos BCrypt aquí: el secreto ya es aleatorio de 256 bits; BCrypt es para passwords humanas débiles.
    /// </summary>
    private static string HashTokenSha256(string tokenPlano)
    {
        var bytes = Encoding.UTF8.GetBytes(tokenPlano);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Base64 estándar con alfabeto URL-safe y sin padding (=).
    /// </summary>
    private static string ToBase64Url(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
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
            NombreUsuario = usuario.NombreUsuario,
            Email = usuario.Email,
            PreferenciaDietetica = usuario.PreferenciaDietetica,
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
