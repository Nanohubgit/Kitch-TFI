namespace Kitch.Domain.Constants;

public static class RolUsuario
{
    public const string Basico = "Basico";
    public const string Profesional = "Profesional";
    public const string Admin = "Admin";

    public static bool EsValido(string rol) =>
        rol is Basico or Profesional or Admin;

    /// <summary>
    /// Profesional o Admin: sin límites de plan Básico.
    /// </summary>
    public static bool TieneAccesoPremium(string? rol) =>
        rol is Profesional or Admin;
}
