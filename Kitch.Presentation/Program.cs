using System.Text;
using Kitch.Presentation.Extensions;
using Kitch.Presentation.Middleware;
using Kitch.Application;
using Kitch.Infrastructure;
using Kitch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];
corsOrigins = corsOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (corsOrigins.Length == 0)
{
    corsOrigins = ["http://localhost:5173"];
}

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
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Tenés que iniciar sesión",
                    Detail = "Tenés que iniciar sesión para acceder a este recurso.",
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
                    Title = "Plan insuficiente",
                    Detail = "No tenés permiso para esta acción. Si acabás de pasar a Profesional, cerrá sesión y volvé a entrar para que se actualice tu plan.",
                    Instance = context.Request.Path
                });
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await app.MigrateDatabaseAsync();
await app.SeedAdminUserAsync();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
