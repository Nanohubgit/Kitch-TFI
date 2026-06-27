using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Presentation.Extensions;

public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Siembra el usuario administrador inicial en tiempo de arranque (no en migraciones).
    /// Ni el email ni la contraseña están hardcodeados: se leen de la configuración
    /// (User Secrets / variables de entorno) bajo las claves "AdminConfig:Email" y "AdminConfig:Password".
    /// </summary>
    public static async Task SeedAdminUserAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<KitchDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        var adminEmail = configuration["AdminConfig:Email"];
        var adminPassword = configuration["AdminConfig:Password"];

        // Sin email o contraseña configurados no sembramos: evitamos crear un admin incompleto
        // y evitamos hardcodear nada. Se avisa por log para que el operador lo configure.
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            logger.LogWarning(
                "No se sembró el administrador: falta la clave 'AdminConfig:Email' en la configuración.");
            return;
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "No se sembró el administrador: falta la clave 'AdminConfig:Password' en la configuración segura.");
            return;
        }

        // Si el admin ya existe, no hacemos nada (idempotente).
        if (await context.Usuarios.AnyAsync(usuario => usuario.Email == adminEmail))
        {
            return;
        }

        var admin = new Usuario
        {
            Nombre = "Admin",
            Apellido = "Kitch",
            Email = adminEmail,
            // El hash se genera en runtime a partir de la contraseña leída de configuración.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Activo = true,
            Rol = RolUsuario.Admin
        };

        await context.Usuarios.AddAsync(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Usuario administrador inicial sembrado correctamente ({Email}).", adminEmail);
    }
}
