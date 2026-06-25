using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class PagoConfiguration : IEntityTypeConfiguration<Pago>
    {
        public void Configure(EntityTypeBuilder<Pago> entity)
        {
            entity.ToTable("Pago");

            entity.HasKey(pago => pago.Id);

            entity.Property(pago => pago.UsuarioId)
                .IsRequired();

            entity.Property(pago => pago.Monto)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(pago => pago.FechaPago)
                .IsRequired();

            entity.HasOne(pago => pago.Usuario)
                .WithMany()
                .HasForeignKey(pago => pago.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pago => pago.ContratoSub)
                .WithMany()
                .HasForeignKey(pago => pago.ContratoSubId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(pago => pago.EstadoPago)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(pago => pago.MetodoPago)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            entity.HasIndex(pago => pago.UsuarioId);
            entity.HasIndex(pago => pago.FechaPago);
            entity.HasIndex(pago => new
            {
                pago.UsuarioId,
                pago.FechaPago
            });
        }
    }
}