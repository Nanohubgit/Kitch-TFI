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

## Deploy en producción (Azure App Service + GitHub Actions)

El workflow `.github/workflows/deploy.yml` compila, publica y despliega la API a Azure App Service (`AZURE_WEBAPP_NAME: kitch`) al hacer `push` a `main` (o manualmente desde `workflow_dispatch`) usando el secret `AZURE_WEBAPP_PUBLISH_PROFILE` en GitHub.

### Variables de ambiente a cargar en Azure App Service (Settings -> Environment variables)

En Azure App Service Linux usar `__` (doble guion bajo) como separador de secciones (también funciona en Windows):

| Variable | Descripción |
|---|---|
| `ConnectionStrings__DefaultConnection` | Connection string real de SQL Server (reemplaza el placeholder) |
| `Jwt__Key` | Clave secreta JWT (>= 32 chars; recomendado referenciar desde Azure Key Vault) |
| `EmailSettings__Password` | API Key de SendGrid |
| `EmailSettings__From` | Remitente verificado exactamente en SendGrid |
| `AdminConfig__Password` | Contraseña del usuario administrador sembrado al iniciar |
| `OpenAi__ApiKey` | API Key de OpenAI (modelo por defecto: `gpt-4o-mini`) |
| `FrontendUrl` | URL pública de Vercel **sin barra final** (ej. `https://kitch.vercel.app`). Se usa para el enlace de recupero y se suma automáticamente a CORS |
| `Cors__AllowedOrigins` | Opcional: orígenes extra separados por coma si hay más de un dominio |
| `MercadoPago__AccessToken` | Access Token de Mercado Pago |
| `MercadoPago__FrontSuccessUrl` | `https://<tu-frontend>/pago-exitoso` |
| `MercadoPago__FrontFailureUrl` | `https://<tu-frontend>/pago-exitoso` |
| `MercadoPago__NotificationUrl` | `https://<tu-backend>/api/Suscripciones/webhook` |
| `MercadoPago__WebhookSecret` | Clave secreta de webhooks del panel de Mercado Pago |

**No cargar `Testing__SinLimitesDePlan=true` en Azure.**

### Verificación post-deploy

- `GET https://<tu-backend>/health` -> debe responder `200 OK` con `{"status":"ok"}`.
- Swagger se deshabilita automáticamente fuera de `Development`.
