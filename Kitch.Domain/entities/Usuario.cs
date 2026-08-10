namespace Kitch.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;

        /// <summary>
        /// Identificador público único para login/mención (distinto del email y del nombre real).
        /// </summary>
        public string NombreUsuario { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Preferencia o restricción dietética del usuario (ej: "Ninguna", "Vegano", "Celiaco").
        /// </summary>
        public string PreferenciaDietetica { get; set; } = "Ninguna";

        /// <summary>
        /// Hash BCrypt de la contraseña del usuario. Nunca se almacena la contraseña en texto plano.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
        public string Rol { get; set; } = "Basico";

        /// <summary>
        /// Hash del token de recuperación de contraseña (nullable: sin reset pendiente).
        /// Seguridad: se guarda el HASH y no el token en texto plano. Si la base de datos se filtra,
        /// un atacante no puede reutilizar los tokens de los emails: no dispone del valor original
        /// que viaja por correo. Al validar el reset se hashea el token recibido y se compara con este valor.
        /// </summary>
        public string? PasswordResetTokenHash { get; set; }

        /// <summary>
        /// Fecha/hora UTC hasta la cual el token de recuperación es válido (nullable: sin reset pendiente).
        /// Tokens expirados se rechazan aunque el hash coincida, limitando la ventana de ataque.
        /// </summary>
        public DateTime? PasswordResetTokenExpiresAt { get; set; }
    }
}
