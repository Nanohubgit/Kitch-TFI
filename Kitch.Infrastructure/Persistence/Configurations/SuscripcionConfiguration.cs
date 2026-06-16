using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{   
     public class SuscripcionConfiguration : IEntityTypeConfiguration<Suscripcion>
   {
        public void Configure(EntityTypeBuilder<Suscripcion> entity)
        {
            entity.ToTable("Suscripcion");

            entity.HasKey(suscripcion => suscripcion.Id);

            entity.Property(suscripcion => suscripcion.UsuarioId)
                .IsRequired();

            entity.Property(suscripcion => suscripcion.FechaInicio)
                .IsRequired();

            entity.Property(suscripcion => suscripcion.FechaFin);

            entity.Property(suscripcion => suscripcion.Activa)
                .IsRequired();

            entity.Property(suscripcion => suscripcion.Tipo)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(suscripcion => suscripcion.Usuario)
                .WithMany()
                .HasForeignKey(suscripcion => suscripcion.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(suscripcion => suscripcion.UsuarioId);
        }
    }
}
