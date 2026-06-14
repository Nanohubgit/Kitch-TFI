using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations;

public class IngredienteRecetaConfiguration : IEntityTypeConfiguration<IngredienteReceta>
{
    public void Configure(EntityTypeBuilder<IngredienteReceta> entity)
    {
        entity.ToTable("IngredienteReceta");

        entity.HasKey(ingredienteReceta => ingredienteReceta.Id);

        entity.Property(ingredienteReceta => ingredienteReceta.RecetaId)
            .IsRequired();

        entity.Property(ingredienteReceta => ingredienteReceta.IngredienteId)
            .IsRequired();

        entity.Property(ingredienteReceta => ingredienteReceta.Cantidad)
            .HasPrecision(10, 2)
            .IsRequired();

        entity.Property(ingredienteReceta => ingredienteReceta.UnidadMedida)
            .IsRequired()
            .HasMaxLength(50);

        entity.HasOne(ingredienteReceta => ingredienteReceta.Receta)
            .WithMany(receta => receta.IngredientesReceta)
            .HasForeignKey(ingredienteReceta => ingredienteReceta.RecetaId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(ingredienteReceta => ingredienteReceta.Ingrediente)
            .WithMany(ingrediente => ingrediente.IngredientesReceta)
            .HasForeignKey(ingredienteReceta => ingredienteReceta.IngredienteId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(ingredienteReceta => ingredienteReceta.RecetaId);

        entity.HasIndex(ingredienteReceta => ingredienteReceta.IngredienteId);

        entity.HasIndex(ingredienteReceta => new
        {
            ingredienteReceta.RecetaId,
            ingredienteReceta.IngredienteId
        }).IsUnique();
    }
}