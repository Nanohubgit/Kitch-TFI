using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class IngredienteConfiguration : IEntityTypeConfiguration<Ingrediente>
    {
        public void Configure(EntityTypeBuilder<Ingrediente> entity)
        {
            entity.ToTable("Ingrediente");

            entity.HasKey(ingrediente => ingrediente.Id);

            entity.Property(ingrediente => ingrediente.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(ingrediente => ingrediente.Descripcion)
                .HasMaxLength(500);
                
            entity.HasIndex(ingrediente => ingrediente.Nombre)
                .IsUnique();
        }
    }
}
