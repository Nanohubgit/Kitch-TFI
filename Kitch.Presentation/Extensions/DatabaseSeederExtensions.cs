using Kitch.Application.Interfaces;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Presentation.Extensions;

public static class DatabaseSeederExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KitchDbContext>();
        await context.Database.MigrateAsync();
    }

    public static async Task NormalizarIngredientesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<KitchDbContext>();
        var normalizer = services.GetRequiredService<IIngredienteNormalizerService>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        var ingredientes = await context.Ingredientes
            .AsNoTracking()
            .Select(ingrediente => new { ingrediente.Id, ingrediente.Nombre })
            .ToListAsync();

        var porNombre = ingredientes
            .GroupBy(ingrediente => ingrediente.Nombre.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var ingrediente in ingredientes)
        {
            var canonico = NormalizarSeguro(normalizer, ingrediente.Nombre);
            if (canonico is null)
            {
                continue;
            }

            var legado = CorregirPluralLegado(normalizer, canonico);
            int? destinoId = null;
            string? nuevoNombre = null;
            var nombreDestino = canonico;

            if (legado != canonico && porNombre.TryGetValue(legado, out var idLegado) && idLegado != ingrediente.Id)
            {
                destinoId = idLegado;
                nombreDestino = legado;
            }
            else if (!string.Equals(canonico, ingrediente.Nombre, StringComparison.Ordinal))
            {
                if (porNombre.TryGetValue(canonico, out var idCanonico) && idCanonico != ingrediente.Id)
                {
                    destinoId = idCanonico;
                }
                else
                {
                    nuevoNombre = canonico;
                }
            }

            if (destinoId is null && nuevoNombre is null)
            {
                continue;
            }

            try
            {
                var estrategia = context.Database.CreateExecutionStrategy();
                await estrategia.ExecuteAsync(async () =>
                {
                    await using var transaccion = await context.Database.BeginTransactionAsync();
                    if (destinoId is int destino)
                    {
                        await FusionarIngredienteAsync(context, ingrediente.Id, destino);
                    }
                    else
                    {
                        var entidad = await context.Ingredientes.FirstAsync(item => item.Id == ingrediente.Id);
                        entidad.Nombre = nuevoNombre!;
                        await context.SaveChangesAsync();
                    }

                    await transaccion.CommitAsync();
                });

                porNombre.Remove(ingrediente.Nombre.Trim());
                if (nuevoNombre is not null)
                {
                    porNombre[nuevoNombre] = ingrediente.Id;
                }

                logger.LogInformation(
                    "Ingrediente '{Nombre}' normalizado a '{Destino}'.",
                    ingrediente.Nombre,
                    nombreDestino);
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                logger.LogWarning(ex, "No se pudo normalizar el ingrediente '{Nombre}'.", ingrediente.Nombre);
            }
        }
    }

    private static string? NormalizarSeguro(IIngredienteNormalizerService normalizer, string nombre)
    {
        try
        {
            return normalizer.Normalizar(nombre);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string CorregirPluralLegado(IIngredienteNormalizerService normalizer, string nombre)
    {
        var palabras = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < palabras.Length; i++)
        {
            var palabra = palabras[i];
            if (!palabra.EndsWith('e'))
            {
                continue;
            }

            var alternativa = normalizer.Normalizar(palabra + "s");
            if (alternativa != palabra && alternativa != palabra + "s")
            {
                palabras[i] = alternativa;
            }
        }

        return string.Join(' ', palabras);
    }

    private static async Task FusionarIngredienteAsync(KitchDbContext context, int origenId, int destinoId)
    {
        var usosReceta = await context.IngredientesReceta
            .Where(item => item.IngredienteId == origenId || item.IngredienteId == destinoId)
            .ToListAsync();

        foreach (var uso in usosReceta.Where(item => item.IngredienteId == origenId).ToList())
        {
            var existente = usosReceta.FirstOrDefault(item =>
                item.RecetaId == uso.RecetaId && item.IngredienteId == destinoId);

            if (existente is null)
            {
                continue;
            }

            if (UnidadesMedida.TryConvertir(uso.Cantidad, uso.UnidadMedida, existente.UnidadMedida, out var cantidad))
            {
                existente.Cantidad += cantidad;
            }

            context.IngredientesReceta.Remove(uso);
            usosReceta.Remove(uso);
        }

        var stocks = await context.StockUsuarios
            .Where(item => item.IngredienteId == origenId || item.IngredienteId == destinoId)
            .ToListAsync();

        foreach (var stock in stocks.Where(item => item.IngredienteId == origenId).ToList())
        {
            var existente = stocks.FirstOrDefault(item =>
                item.UsuarioId == stock.UsuarioId && item.IngredienteId == destinoId);

            if (existente is null)
            {
                continue;
            }

            if (UnidadesMedida.TryConvertir(stock.Cantidad, stock.UnidadMedida, existente.UnidadMedida, out var cantidad))
            {
                existente.Cantidad += cantidad;
            }

            if (stock.FechaCaducidad is not null &&
                (existente.FechaCaducidad is null || stock.FechaCaducidad < existente.FechaCaducidad))
            {
                existente.FechaCaducidad = stock.FechaCaducidad;
            }

            context.StockUsuarios.Remove(stock);
            stocks.Remove(stock);
        }

        var sustitutos = await context.SustitutosIngrediente
            .Where(item =>
                item.IngredienteOriginalId == origenId || item.IngredienteSustitutoId == origenId ||
                item.IngredienteOriginalId == destinoId || item.IngredienteSustitutoId == destinoId)
            .ToListAsync();

        var paresVigentes = sustitutos
            .Where(item => item.IngredienteOriginalId != origenId && item.IngredienteSustitutoId != origenId)
            .Select(item => (item.IngredienteOriginalId, item.IngredienteSustitutoId))
            .ToHashSet();

        var sustitutosAMover = new List<(SustitutoIngrediente Sustituto, int Original, int Reemplazo)>();
        foreach (var sustituto in sustitutos.Where(item =>
                     item.IngredienteOriginalId == origenId || item.IngredienteSustitutoId == origenId))
        {
            var original = sustituto.IngredienteOriginalId == origenId ? destinoId : sustituto.IngredienteOriginalId;
            var reemplazo = sustituto.IngredienteSustitutoId == origenId ? destinoId : sustituto.IngredienteSustitutoId;

            if (original == reemplazo || !paresVigentes.Add((original, reemplazo)))
            {
                context.SustitutosIngrediente.Remove(sustituto);
            }
            else
            {
                sustitutosAMover.Add((sustituto, original, reemplazo));
            }
        }

        await context.SaveChangesAsync();

        foreach (var uso in usosReceta.Where(item => item.IngredienteId == origenId))
        {
            uso.IngredienteId = destinoId;
        }

        foreach (var stock in stocks.Where(item => item.IngredienteId == origenId))
        {
            stock.IngredienteId = destinoId;
        }

        foreach (var (sustituto, original, reemplazo) in sustitutosAMover)
        {
            sustituto.IngredienteOriginalId = original;
            sustituto.IngredienteSustitutoId = reemplazo;
        }

        var items = await context.ItemsListaCompra
            .Where(item => item.IngredienteId == origenId)
            .ToListAsync();

        foreach (var item in items)
        {
            item.IngredienteId = destinoId;
        }

        await context.SaveChangesAsync();

        var origen = await context.Ingredientes.FirstAsync(item => item.Id == origenId);
        context.Ingredientes.Remove(origen);
        await context.SaveChangesAsync();
    }

    public static async Task SeedAdminUserAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<KitchDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        var candidatos = new List<AdminSeedDto>();
        var principal = new AdminSeedDto
        {
            Email = configuration["AdminConfig:Email"],
            Password = configuration["AdminConfig:Password"],
            NombreUsuario = configuration["AdminConfig:NombreUsuario"],
            Nombre = "Admin",
            Apellido = "Kitch"
        };
        candidatos.Add(principal);

        var extras = configuration.GetSection("AdminConfig:Admins").Get<List<AdminSeedDto>>() ?? [];
        candidatos.AddRange(extras);

        foreach (var candidato in candidatos)
        {
            await SembrarAdminSiCorrespondeAsync(context, logger, candidato);
        }
    }

    private static async Task SembrarAdminSiCorrespondeAsync(
        KitchDbContext context,
        ILogger logger,
        AdminSeedDto candidato)
    {
        var email = candidato.Email?.Trim();
        var password = candidato.Password;

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var existente = await context.Usuarios.FirstOrDefaultAsync(usuario => usuario.Email == email);
        if (existente is not null)
        {
            if (existente.Rol != RolUsuario.Admin || !existente.Activo)
            {
                existente.Rol = RolUsuario.Admin;
                existente.Activo = true;
                await context.SaveChangesAsync();
                logger.LogInformation("Usuario existente promovido a administrador ({Email}).", email);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var nombreUsuario = await ResolverNombreUsuarioLibreAsync(
            context,
            string.IsNullOrWhiteSpace(candidato.NombreUsuario)
                ? email.Split('@')[0]
                : candidato.NombreUsuario.Trim());

        var admin = new Usuario
        {
            Nombre = string.IsNullOrWhiteSpace(candidato.Nombre) ? "Admin" : candidato.Nombre.Trim(),
            Apellido = string.IsNullOrWhiteSpace(candidato.Apellido) ? "Kitch" : candidato.Apellido.Trim(),
            NombreUsuario = nombreUsuario,
            Email = email,
            PreferenciaDietetica = "Ninguna",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Activo = true,
            Rol = RolUsuario.Admin
        };

        await context.Usuarios.AddAsync(admin);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Usuario administrador inicial sembrado ({Email}, {NombreUsuario}).",
            email,
            nombreUsuario);
    }

    private static async Task<string> ResolverNombreUsuarioLibreAsync(KitchDbContext context, string deseado)
    {
        var baseNombre = string.IsNullOrWhiteSpace(deseado) ? "admin_alacena" : deseado.Trim();
        if (baseNombre.Length > 45)
        {
            baseNombre = baseNombre[..45];
        }
        var candidato = baseNombre;
        var sufijo = 1;

        while (await context.Usuarios.AnyAsync(usuario => usuario.NombreUsuario == candidato))
        {
            sufijo++;
            candidato = $"{baseNombre}{sufijo}";
        }

        return candidato;
    }

    private sealed class AdminSeedDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? NombreUsuario { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
    }
}
