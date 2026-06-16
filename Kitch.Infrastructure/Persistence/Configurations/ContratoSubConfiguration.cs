using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class ContratoSubConfiguration : IEntityTypeConfiguration<ContratoSub>
    {
        public void Configure(EntityTypeBuilder<ContratoSub> entity)
        {
            entity.ToTable("ContratoSub");

            entity.HasKey(contratoSub => contratoSub.Id);

            entity.Property(contratoSub => contratoSub.UsuarioId)
                .IsRequired();

            entity.Property(contratoSub => contratoSub.SuscripcionId)
                .IsRequired();

            entity.Property(contratoSub => contratoSub.FechaContratacion)
                .IsRequired();

            entity.Property(contratoSub => contratoSub.FechaInicio)
                .IsRequired();

            entity.Property(contratoSub => contratoSub.FechaFin);

            entity.Property(contratoSub => contratoSub.Monto)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(contratoSub => contratoSub.Estado)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(contratoSub => contratoSub.Usuario)
                .WithMany()
                .HasForeignKey(contratoSub => contratoSub.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(contratoSub => contratoSub.Suscripcion)
                .WithMany()
                .HasForeignKey(contratoSub => contratoSub.SuscripcionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(contratoSub => contratoSub.UsuarioId);
            entity.HasIndex(contratoSub => contratoSub.SuscripcionId);
        }
    }
}