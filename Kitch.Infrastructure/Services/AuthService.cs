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
    private const int TwoFactorCodeExpirationMinutes = 5;

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

    public async Task<Login2FaResponseDto> LoginAsync(LoginRequest request)
    {
        if (request is null)
        {
            throw new UnauthorizedAccessException("Usuario/email o contraseña inválidos.");
        }

        var usuarioOMail = request.UsuarioOMail?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(usuarioOMail) || string.IsNullOrWhiteSpace(password))
        {
            throw new UnauthorizedAccessException("Usuario/email o contraseña inválidos.");
        }

        // Login flexible: identificador puede ser Email o NombreUsuario.
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
            u.Email == usuarioOMail || u.NombreUsuario == usuarioOMail);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
        {
            throw new UnauthorizedAccessException("Usuario/email o contraseña inválidos.");
        }

        if (!usuario.Activo)
        {
            throw new UnauthorizedAccessException("El usuario se encuentra inactivo.");
        }

        // Primer factor OK. No se emite JWT hasta verificar el código 2FA (Fase 3).
        // Código numérico 000000–999999 con CSPRNG (no Random.Next).
        var codigoPlano = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        // Guardamos SHA-256 del código (misma idea que el token de reset), no el dígito en claro.
        usuario.TwoFactorCode = HashTokenSha256(codigoPlano);
        usuario.TwoFactorCodeExpiresAt = DateTime.UtcNow.AddMinutes(TwoFactorCodeExpirationMinutes);

        await _context.SaveChangesAsync();

        var body =
            "Tu código de verificación para ingresar a Alacena Virtual / Kitch es:\n\n" +
            $"{codigoPlano}\n\n" +
            $"Válido por {TwoFactorCodeExpirationMinutes} minutos. Si no intentaste iniciar sesión, ignorá este correo.";

        await _emailService.SendEmailAsync(
            to: usuario.Email,
            subject: "Código de verificación 2FA — Alacena Virtual",
            body: body);

        return new Login2FaResponseDto
        {
            RequiresTwoFactor = true,
            Email = usuario.Email,
            EmailEnmascarado = EnmascararEmail(usuario.Email),
            Mensaje = "Credenciales correctas. Ingresá el código de 6 dígitos enviado a tu correo."
        };
    }

    public async Task<Verify2FaResponseDto> Verify2FaAsync(Verify2FaRequestDto request)
    {
        if (request is null)
        {
            throw new InvalidOperationException("Email y código de verificación son obligatorios.");
        }

        var email = request.Email?.Trim() ?? string.Empty;
        var codigo = request.Codigo?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(codigo))
        {
            throw new InvalidOperationException("Email y código de verificación son obligatorios.");
        }

        if (codigo.Length != 6 || !codigo.All(char.IsDigit))
        {
            throw new InvalidOperationException("El código debe tener exactamente 6 dígitos.");
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        var codigoHash = HashTokenSha256(codigo);

        // Mensaje único: no filtrar si falló email, código o expiración.
        if (usuario is null ||
            string.IsNullOrEmpty(usuario.TwoFactorCode) ||
            usuario.TwoFactorCodeExpiresAt is null ||
            usuario.TwoFactorCodeExpiresAt <= DateTime.UtcNow ||
            !FixedTimeEquals(usuario.TwoFactorCode, codigoHash))
        {
            throw new InvalidOperationException("Código inválido o expirado. Solicitá un nuevo código iniciando sesión otra vez.");
        }

        // Un solo uso: limpiar OTP antes de emitir sesión.
        usuario.TwoFactorCode = null;
        usuario.TwoFactorCodeExpiresAt = null;
        await _context.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpiresInMinutes());
        var token = GenerateJwtToken(usuario, expiresAt);

        return new Verify2FaResponseDto
        {
            Token = token,
            Mensaje = "Login exitoso",
            ExpiresAt = expiresAt,
            Email = usuario.Email,
            NombreUsuario = usuario.NombreUsuario,
            Rol = usuario.Rol
        };
    }

    public async Task<PerfilUsuarioResponseDto?> GetPerfilAsync(int usuarioId)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
        {
            return null;
        }

        return await MapPerfilAsync(usuario);
    }

    public async Task<PerfilUsuarioResponseDto> EditarPerfilAsync(int usuarioId, EditarPerfilRequestDto request)
    {
        if (request is null)
        {
            throw new InvalidOperationException("Los datos del perfil son obligatorios.");
        }

        var nombreUsuario = request.NombreUsuario?.Trim() ?? string.Empty;
        var preferencia = string.IsNullOrWhiteSpace(request.PreferenciaDietetica)
            ? "Ninguna"
            : request.PreferenciaDietetica.Trim();

        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new InvalidOperationException("El nombre de usuario es obligatorio.");
        }

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
        {
            throw new InvalidOperationException("Usuario no encontrado.");
        }

        if (!string.Equals(usuario.NombreUsuario, nombreUsuario, StringComparison.Ordinal) &&
            await NombreUsuarioExisteAsync(nombreUsuario))
        {
            throw new InvalidOperationException("El nombre de usuario ya está en uso.");
        }

        usuario.NombreUsuario = nombreUsuario;
        usuario.PreferenciaDietetica = preferencia;
        await _context.SaveChangesAsync();

        return await MapPerfilAsync(usuario);
    }

    private async Task<PerfilUsuarioResponseDto> MapPerfilAsync(Usuario usuario)
    {
        var ahora = DateTime.UtcNow;

        // Suscripción propia activa con FechaFin vigente (o sin fin = premium abierto).
        var suscripcionActivaHasta = await _context.Suscripciones
            .AsNoTracking()
            .Where(s => s.UsuarioId == usuario.Id && s.Activa)
            .Where(s => s.FechaFin == null || s.FechaFin > ahora)
            .OrderByDescending(s => s.FechaFin)
            .Select(s => s.FechaFin)
            .FirstOrDefaultAsync();

        return new PerfilUsuarioResponseDto
        {
            Id = usuario.Id,
            Email = usuario.Email,
            NombreUsuario = usuario.NombreUsuario,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Rol = usuario.Rol,
            SuscripcionActivaHasta = suscripcionActivaHasta,
            PreferenciaDietetica = usuario.PreferenciaDietetica
        };
    }

    /// <summary>
    /// Comparación en tiempo constante para hashes hex (evita timing attacks triviales).
    /// </summary>
    private static bool FixedTimeEquals(string storedHash, string providedHash)
    {
        var a = Encoding.UTF8.GetBytes(storedHash);
        var b = Encoding.UTF8.GetBytes(providedHash);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    /// <summary>
    /// Enmascara un email para la UI: a***z@dominio.com (sin filtrar el dominio completo de más).
    /// </summary>
    private static string EnmascararEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "***";
        }

        var parts = email.Split('@', 2);
        var local = parts[0];
        var domain = parts[1];

        if (local.Length <= 1)
        {
            return $"*@{domain}";
        }

        if (local.Length == 2)
        {
            return $"{local[0]}*@{domain}";
        }

        return $"{local[0]}***{local[^1]}@{domain}";
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
