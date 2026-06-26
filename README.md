# Kitch-TFI

## Configuración

La clave JWT no está en el repositorio. Cada quien debe configurarla localmente con User Secrets:

```bash
dotnet user-secrets set "Jwt:Key" "<clave-de-al-menos-32-chars>" --project "Kitch.Presentation/Kitch.Presentation.csproj"
```
