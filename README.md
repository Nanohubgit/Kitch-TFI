# Kitch-TFI (Backend API)

API REST en **.NET 10** con **Clean Architecture** (`Kitch.Domain`, `Kitch.Application`, `Kitch.Infrastructure`, `Kitch.Presentation`), **Entity Framework Core** (SQL Server, Code First) e integraciones reales con **OpenAI** (`IHttpClientFactory`), **SendGrid** (SMTP 2FA y recupero) y **Mercado Pago** (Checkout Pro + webhook).

## Desarrollo local (`localhost`)

1. Configurar secretos locales con User Secrets (nunca commitear secretos en `appsettings.json`):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<tu-sql-server-local>" --project "Kitch.Presentation/Kitch.Presentation.csproj"
dotnet user-secrets set "Jwt:Key" "<clave-de-al-menos-32-caracteres>" --project "Kitch.Presentation/Kitch.Presentation.csproj"
dotnet user-secrets set "EmailSettings:Password" "<sendgrid-api-key>" --project "Kitch.Presentation/Kitch.Presentation.csproj"
dotnet user-secrets set "EmailSettings:From" "<remitente-verificado-en-sendgrid>" --project "Kitch.Presentation/Kitch.Presentation.csproj"
dotnet user-secrets set "AdminConfig:Password" "<password-admin-inicial>" --project "Kitch.Presentation/Kitch.Presentation.csproj"
dotnet user-secrets set "OpenAi:ApiKey" "<openai-api-key>" --project "Kitch.Presentation/Kitch.Presentation.csproj"
dotnet user-secrets set "MercadoPago:AccessToken" "<mp-access-token>" --project "Kitch.Presentation/Kitch.Presentation.csproj"
```

2. Ejecutar localmente (`ASPNETCORE_ENVIRONMENT=Development`, puerto `http://localhost:5295`):

```bash
dotnet run --project Kitch.Presentation/Kitch.Presentation.csproj
```

En `Development`, `Kitch.Presentation/appsettings.Development.json` apunta automáticamente a `http://localhost:5173` (recupero y vuelta de Mercado Pago) y habilita `Testing:SinLimitesDePlan=true` solo para pruebas locales.

## Modo test (`Testing:SinLimitesDePlan`)

- En **local (`Development`)**: está en `true` en `appsettings.Development.json` (sin tope diario de consultas IA ni de recetas guardadas por IA en plan Básico; dificultad, favoritos, planificador y sustitutos siguen limitados). Para probar en local con límites estrictos de producción, ponelo en `false` y reiniciá la API.
- En **producción (`Production`)**: está **desactivado**. `Program.cs` aborta el arranque si alguien intenta activarlo fuera de `Development`.

