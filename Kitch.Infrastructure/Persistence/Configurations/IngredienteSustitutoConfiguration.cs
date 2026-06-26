using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class IngredienteSustitutoConfiguration : IEntityTypeConfiguration<IngredienteSustituto>
    {
        public void Configure(EntityTypeBuilder<IngredienteSustituto> entity)
        {
            entity.ToTable("IngredienteSustituto");

            entity.HasKey(sustituto => sustituto.Id);

            entity.Property(sustituto => sustituto.IngredienteId)
                .IsRequired();

            entity.Property(sustituto => sustituto.SustitutoId)
                .IsRequired();

            entity.Property(sustituto => sustituto.Motivo)
                .HasMaxLength(500);

            entity.HasOne(sustituto => sustituto.Ingrediente)
                .WithMany(ingrediente => ingrediente.Sustitutos)
                .HasForeignKey(sustituto => sustituto.IngredienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sustituto => sustituto.Sustituto)
                .WithMany(ingrediente => ingrediente.SustitucionesDe)
                .HasForeignKey(sustituto => sustituto.SustitutoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(sustituto => sustituto.IngredienteId);
            entity.HasIndex(sustituto => sustituto.SustitutoId);
            entity.HasIndex(sustituto => new
            {
                sustituto.IngredienteId,
                sustituto.SustitutoId
            }).IsUnique();
        }
    }
}
