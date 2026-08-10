using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> entity)
        {
            entity.ToTable("Usuario");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Apellido)
                .IsRequired()
                .HasMaxLength(100);

            // Identificador único legible: obligatorio a nivel de DB (+ índice único).
            entity.Property(x => x.NombreUsuario)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(x => x.NombreUsuario)
                .IsUnique();

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(x => x.PreferenciaDietetica)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Ninguna");

            entity.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);

            // SHA-256 en hex = 64 caracteres. Nullable: sin reset pendiente.
            entity.Property(x => x.PasswordResetTokenHash)
                .HasMaxLength(64);

            // UTC en aplicación; columna opcional hasta que haya un forgot activo.
            entity.Property(x => x.PasswordResetTokenExpiresAt);

            entity.Property(x => x.Activo)
                .IsRequired();

            entity.Property(x => x.Rol)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            // Búsqueda O(1) al resetear: se busca por hash del token, no por scan completo.
            entity.HasIndex(x => x.PasswordResetTokenHash);
        }
    }
}
