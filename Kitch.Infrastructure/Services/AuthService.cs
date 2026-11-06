using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kitch.Application.DTOs.Auth;
using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Kitch.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kitch.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const int PasswordResetTokenBytes = 32;
    private const int PasswordResetExpirationMinutes = 30;
    private const int TwoFactorCodeExpirationMinutes = 5;
    private const int MaxIntentosDosFactores = 5;

    private readonly KitchDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ISuscripcionVigenciaService _suscripcionVigencia;
    private readonly IMemoryCache _cache;

    public AuthService(
        KitchDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        ISuscripcionVigenciaService suscripcionVigencia,
        IMemoryCache cache)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _suscripcionVigencia = suscripcionVigencia;
        _cache = cache;
    }

    public async Task<Login2FaResponseDto> RegisterAsync(RegisterRequest request)
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

        try
        {
            return await EnviarCodigoDosFactoresAsync(
                usuario,
                "Cuenta creada. Ingresá el código de 6 dígitos enviado a tu correo para iniciar sesión.");
        }
        catch (ServicioExternoException)
        {
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            throw;
        }
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

        var usuario = await FindUsuarioPorIdentificadorAsync(usuarioOMail);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
        {
            throw new UnauthorizedAccessException("Usuario/email o contraseña inválidos.");
        }

        if (!usuario.Activo)
        {
            throw new UnauthorizedAccessException("El usuario se encuentra inactivo.");
        }

        return await EnviarCodigoDosFactoresAsync(
            usuario,
            "Credenciales correctas. Ingresá el código de 6 dígitos enviado a tu correo.");
    }

    private async Task<Login2FaResponseDto> EnviarCodigoDosFactoresAsync(Usuario usuario, string mensaje)
    {
        var codigoPlano = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        usuario.TwoFactorCode = HashTokenSha256(codigoPlano);
        usuario.TwoFactorCodeExpiresAt = DateTime.UtcNow.AddMinutes(TwoFactorCodeExpirationMinutes);
        await _context.SaveChangesAsync();
        _cache.Remove(ClaveIntentosDosFactores(usuario.Email));

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
            Mensaje = mensaje
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

        if (usuario is null ||
            string.IsNullOrEmpty(usuario.TwoFactorCode) ||
            usuario.TwoFactorCodeExpiresAt is null ||
            usuario.TwoFactorCodeExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Código inválido o expirado. Solicitá un nuevo código iniciando sesión otra vez.");
        }

        if (!FixedTimeEquals(usuario.TwoFactorCode, codigoHash))
        {
            var claveIntentos = ClaveIntentosDosFactores(usuario.Email);
            var intentos = _cache.TryGetValue(claveIntentos, out int previos) ? previos + 1 : 1;

            if (intentos >= MaxIntentosDosFactores)
            {
                usuario.TwoFactorCode = null;
                usuario.TwoFactorCodeExpiresAt = null;
                await _context.SaveChangesAsync();
                _cache.Remove(claveIntentos);
                throw new InvalidOperationException(
                    "Superaste la cantidad de intentos permitidos. Iniciá sesión otra vez para recibir un código nuevo.");
            }

            _cache.Set(claveIntentos, intentos, TimeSpan.FromMinutes(TwoFactorCodeExpirationMinutes));
            throw new InvalidOperationException(
                $"Código incorrecto. Te quedan {MaxIntentosDosFactores - intentos} intentos.");
        }

        _cache.Remove(ClaveIntentosDosFactores(usuario.Email));
        usuario.TwoFactorCode = null;
        usuario.TwoFactorCodeExpiresAt = null;
        await _suscripcionVigencia.AsegurarRolVigenteAsync(usuario);
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
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
        {
            return null;
        }

        await _suscripcionVigencia.AsegurarRolVigenteAsync(usuario);
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

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (!string.Equals(usuario.NombreUsuario, nombreUsuario, StringComparison.Ordinal) &&
            await NombreUsuarioExisteAsync(nombreUsuario))
        {
            throw new InvalidOperationException("El nombre de usuario ya está en uso.");
        }

        usuario.NombreUsuario = nombreUsuario;
        usuario.PreferenciaDietetica = preferencia;
        await _suscripcionVigencia.AsegurarRolVigenteAsync(usuario);
        await _context.SaveChangesAsync();

        return await MapPerfilAsync(usuario);
    }

    private async Task<PerfilUsuarioResponseDto> MapPerfilAsync(Usuario usuario)
    {
        var ahora = DateTime.UtcNow;

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
            PeticionesIaDelDia = usuario.FechaCuotaIaUtc?.Date == FechaLocal.Hoy()
                ? usuario.PeticionesIaDelDia
                : 0,
            LimitePeticionesIa = PedidoRecetaPremium.TopeDiario(usuario.Rol),
            PreferenciaDietetica = usuario.PreferenciaDietetica
        };
    }

    private Task<Usuario?> FindUsuarioPorIdentificadorAsync(string usuarioOMail)
    {
        return _context.Usuarios.FirstOrDefaultAsync(u =>
            u.Email == usuarioOMail || u.NombreUsuario == usuarioOMail);
    }

    private static string ClaveIntentosDosFactores(string email) =>
        $"2fa-intentos:{email.Trim().ToLowerInvariant()}";

    private static bool FixedTimeEquals(string storedHash, string providedHash)
    {
        var a = Encoding.UTF8.GetBytes(storedHash);
        var b = Encoding.UTF8.GetBytes(providedHash);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

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
            return;
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        if (usuario is null)
        {
            return;
        }

        await EnviarTokenRecuperacionAsync(usuario);
    }

    public async Task EnviarRecuperacionPasswordAsync(int usuarioId)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (!usuario.Activo)
        {
            throw new InvalidOperationException("El usuario está bloqueado. Desbloquealo antes de enviar la recuperación de contraseña.");
        }

        await EnviarTokenRecuperacionAsync(usuario);
    }

    public async Task<Verify2FaResponseDto> RefrescarSesionAsync(int usuarioId)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null || !usuario.Activo)
        {
            throw new UnauthorizedAccessException("Tu sesión ya no es válida. Iniciá sesión nuevamente.");
        }

        await _suscripcionVigencia.AsegurarRolVigenteAsync(usuario);
        await _context.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpiresInMinutes());

        return new Verify2FaResponseDto
        {
            Token = GenerateJwtToken(usuario, expiresAt),
            Mensaje = "Sesión renovada",
            ExpiresAt = expiresAt,
            Email = usuario.Email,
            NombreUsuario = usuario.NombreUsuario,
            Rol = usuario.Rol
        };
    }

    private async Task EnviarTokenRecuperacionAsync(Usuario usuario)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(PasswordResetTokenBytes);

        var tokenPlano = ToBase64Url(tokenBytes);

        usuario.PasswordResetTokenHash = HashTokenSha256(tokenPlano);
        usuario.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetExpirationMinutes);

        await _context.SaveChangesAsync();

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

        if (nuevaPassword.Length < 8)
        {
            throw new InvalidOperationException("La nueva contraseña debe tener al menos 8 caracteres.");
        }

        var tokenHash = HashTokenSha256(tokenPlano);

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash);

        if (usuario is null ||
            usuario.PasswordResetTokenExpiresAt is null ||
            usuario.PasswordResetTokenExpiresAt <= DateTime.UtcNow ||
            !usuario.Activo)
        {
            throw new InvalidOperationException("Token inválido o expirado.");
        }

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nuevaPassword);

        usuario.PasswordResetTokenHash = null;
        usuario.PasswordResetTokenExpiresAt = null;

        await _context.SaveChangesAsync();
    }

    private static string HashTokenSha256(string tokenPlano)
    {
        var bytes = Encoding.UTF8.GetBytes(tokenPlano);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string ToBase64Url(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
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
