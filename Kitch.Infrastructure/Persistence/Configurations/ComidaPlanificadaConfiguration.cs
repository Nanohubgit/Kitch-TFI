using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class ComidaPlanificadaConfiguration : IEntityTypeConfiguration<ComidaPlanificada>
    {
        public void Configure(EntityTypeBuilder<ComidaPlanificada> entity)
   {
            entity.ToTable("ComidaPlanificada");

            entity.HasKey(comidaPlanificada => comidaPlanificada.Id);

            entity.Property(comidaPlanificada => comidaPlanificada.FechaAsignada)
                .IsRequired();

            entity.Property(comidaPlanificada => comidaPlanificada.Turno)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(comidaPlanificada => comidaPlanificada.UsuarioId)
                .IsRequired();

            entity.Property(comidaPlanificada => comidaPlanificada.RecetaId)
                .IsRequired();

            entity.HasOne(comidaPlanificada => comidaPlanificada.Usuario)
                .WithMany()
                .HasForeignKey(comidaPlanificada => comidaPlanificada.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(comidaPlanificada => comidaPlanificada.Receta)
                .WithMany()
                .HasForeignKey(comidaPlanificada => comidaPlanificada.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(comidaPlanificada => comidaPlanificada.UsuarioId);
            entity.HasIndex(comidaPlanificada => comidaPlanificada.RecetaId);
            entity.HasIndex(comidaPlanificada => new
            {
                comidaPlanificada.UsuarioId,
                comidaPlanificada.FechaAsignada,
                comidaPlanificada.Turno
            });
        }
    }
}
