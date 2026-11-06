namespace Kitch.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PreferenciaDietetica { get; set; } = "Ninguna";

        public string PasswordHash { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
        public string Rol { get; set; } = "Basico";

        public string? PasswordResetTokenHash { get; set; }

        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        public string? TwoFactorCode { get; set; }

        public DateTime? TwoFactorCodeExpiresAt { get; set; }

        public DateTime? FechaCuotaIaUtc { get; set; }

        public int PeticionesIaDelDia { get; set; }

        public string? UltimaRecetaIaJson { get; set; }
    }
}
