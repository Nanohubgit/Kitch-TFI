using System.Text;
using System.Text.Json.Serialization;
using Kitch.Presentation.Extensions;
using Kitch.Presentation.Filters;
using Kitch.Presentation.Middleware;
using Kitch.Application;
using Kitch.Infrastructure;
using Kitch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var detalle = string.Join(" ",
            context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? "Dato inválido."
                    : e.ErrorMessage));

        return new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Pedido inválido",
            Detail = string.IsNullOrWhiteSpace(detalle)
                ? "Revisá los datos enviados."
                : detalle,
            Instance = context.HttpContext.Request.Path
        })
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegá solo el token JWT (sin escribir 'Bearer' adelante)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var corsOrigins = ResolverOrigenesCors(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location");
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<KitchDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

var claveSinLimitesDePlan = Kitch.Infrastructure.DependencyInjection.ClaveSinLimitesDePlan;
var sinLimitesDePlan = bool.TryParse(builder.Configuration[claveSinLimitesDePlan], out var valorSinLimites) && valorSinLimites;
if (sinLimitesDePlan && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        $"'{claveSinLimitesDePlan}' solo se puede activar en el entorno Development.");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key no configurada. Definí 'Jwt:Key' en User Secrets (local) o en la configuración de Azure.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                var bloqueado = context.HttpContext.Items.ContainsKey(RolVigenteClaimsTransformation.ItemUsuarioBloqueado);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Tenés que iniciar sesión",
                    Detail = bloqueado
                        ? "Tu cuenta está bloqueada o ya no existe. Contactá a un administrador."
                        : "Tenés que iniciar sesión para acceder a este recurso.",
                    Instance = context.Request.Path
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Sin permiso",
                    Detail = "No tenés permiso para esta acción con tu plan o rol actual.",
                    Instance = context.Request.Path
                });
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClaimsTransformation, RolVigenteClaimsTransformation>();

var app = builder.Build();

if (sinLimitesDePlan)
{
    app.Logger.LogWarning(
        "Modo pruebas activo ({Clave}): sin tope de recetas guardadas ni de consultas de IA del plan Básico. Dificultad, favoritos, planificador y sustitutos siguen limitados.",
        claveSinLimitesDePlan);
}

await app.MigrateDatabaseAsync();
await app.NormalizarIngredientesAsync();
await app.SeedAdminUserAsync();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();

app.Run();

static string[] ResolverOrigenesCors(IConfiguration configuration)
{
    var origenes = new List<string>();

    var lista = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (lista is { Length: > 0 })
    {
        origenes.AddRange(lista);
    }

    var plano = configuration["Cors:AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(plano))
    {
        origenes.AddRange(plano.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    AgregarOrigenDesdeUrl(origenes, configuration["FrontendUrl"]);
    AgregarOrigenDesdeUrl(origenes, configuration["Frontend:BaseUrl"]);

    var limpios = origenes
        .Select(NormalizarOrigen)
        .Where(origen => !string.IsNullOrWhiteSpace(origen))
        .Select(origen => origen!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    return limpios.Length > 0 ? limpios : ["http://localhost:5173"];
}

static void AgregarOrigenDesdeUrl(List<string> destino, string? url)
{
    var origen = NormalizarOrigen(url);
    if (!string.IsNullOrWhiteSpace(origen))
    {
        destino.Add(origen);
    }
}

static string? NormalizarOrigen(string? valor)
{
    if (string.IsNullOrWhiteSpace(valor))
    {
        return null;
    }

    var limpio = valor.Trim().TrimEnd('/');
    if (Uri.TryCreate(limpio, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
    {
        return uri.GetLeftPart(UriPartial.Authority);
    }

    return null;
}
