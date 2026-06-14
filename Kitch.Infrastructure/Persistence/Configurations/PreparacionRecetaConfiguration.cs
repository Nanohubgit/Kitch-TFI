using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class PreparacionRecetaConfiguration : IEntityTypeConfiguration<PreparacionReceta>
    {
        public void Configure(EntityTypeBuilder<PreparacionReceta> entity)
        {
            entity.ToTable("PreparacionReceta");

            entity.HasKey(preparacionReceta => preparacionReceta.Id);

            entity.Property(preparacionReceta => preparacionReceta.RecetaId)
                .IsRequired();

            entity.Property(preparacionReceta => preparacionReceta.NumeroPaso)
                .IsRequired();

            entity.Property(preparacionReceta => preparacionReceta.DescripcionPaso)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasOne(preparacionReceta => preparacionReceta.Receta)
                .WithMany(receta => receta.Preparaciones)
                .HasForeignKey(preparacionReceta => preparacionReceta.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(preparacionReceta => preparacionReceta.RecetaId);
            entity.HasIndex(preparacionReceta => new
            {
                preparacionReceta.RecetaId,
                preparacionReceta.NumeroPaso
            }).IsUnique();
        }
    }
}