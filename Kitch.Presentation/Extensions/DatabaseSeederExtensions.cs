using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Presentation.Extensions;

public static class DatabaseSeederExtensions
{
    public static async Task SeedAdminUserAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<KitchDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        var adminEmail = configuration["AdminConfig:Email"];
        var adminPassword = configuration["AdminConfig:Password"];

        // Alias de login legible. Configurable para no hardcodear en código de producción.
        var adminNombreUsuario = configuration["AdminConfig:NombreUsuario"];
        if (string.IsNullOrWhiteSpace(adminNombreUsuario))
        {
            adminNombreUsuario = "admin_alacena";
        }

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

        // Idempotencia: si ya existe por email O por nombre de usuario, no insertamos de nuevo.
        if (await context.Usuarios.AnyAsync(usuario =>
                usuario.Email == adminEmail || usuario.NombreUsuario == adminNombreUsuario))
        {
            return;
        }

        var admin = new Usuario
        {
            Nombre = "Admin",
            Apellido = "Kitch",
            NombreUsuario = adminNombreUsuario,
            Email = adminEmail,
            PreferenciaDietetica = "Ninguna",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Activo = true,
            Rol = RolUsuario.Admin
        };

        await context.Usuarios.AddAsync(admin);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Usuario administrador inicial sembrado ({Email}, {NombreUsuario}).",
            adminEmail,
            adminNombreUsuario);
    }
}
