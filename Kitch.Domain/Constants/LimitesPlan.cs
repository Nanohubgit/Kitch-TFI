using Kitch.Domain.Entities;

namespace Kitch.Domain.Constants;

/// <summary>
/// Límites del plan Básico vs Profesional (requisito TFI).
/// </summary>
public static class LimitesPlan
{
    public const int MaxFavoritosBasico = 5;
    public const int MaxComidasPlanificadasBasico = 3;
    public const int DiasPlanificacionBasico = 7;
    public const int DiasPlanificacionProfesional = 30;
    public const int MaxSustitutosBasico = 1;

    public const string MensajeDificultadPremium =
        "Las recetas difíciles son del plan Profesional. Con Básico podés usar Fácil e Intermedia. Pasate a Profesional para desbloquearlas.";

    public const string MensajeLimiteFavoritos =
        "Alcanzaste el límite de recetas guardadas del plan Básico (5). Pasate a Profesional para guardar más.";

    public const string MensajeLimitePlanner =
        "Alcanzaste el límite de comidas planificadas del plan Básico (3). Pasate a Profesional para planificar más.";

    public const string MensajeHorizontePlanner =
        "El plan Básico permite agendar hasta 7 días adelante. Pasate a Profesional para planificar a 30 días.";

    public const string MensajeMasSustitutosPremium =
        "Hay más reemplazos en el plan Profesional. Pasate para ver el resto.";

    public static bool PuedeUsarDificultad(string? rol, DificultadReceta dificultad) =>
        RolUsuario.TieneAccesoPremium(rol) ||
        dificultad is DificultadReceta.Facil or DificultadReceta.Medio;

    public static DificultadReceta AjustarDificultad(string? rol, DificultadReceta dificultad) =>
        PuedeUsarDificultad(rol, dificultad) ? dificultad : DificultadReceta.Medio;
}
