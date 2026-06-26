using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations;

public class SustitutoIngredienteConfiguration : IEntityTypeConfiguration<SustitutoIngrediente>
{
    public void Configure(EntityTypeBuilder<SustitutoIngrediente> entity)
    {
        entity.ToTable("SustitutoIngrediente");

        entity.HasKey(sustituto => sustituto.Id);

        entity.Property(sustituto => sustituto.IngredienteOriginalId)
            .IsRequired();

        entity.Property(sustituto => sustituto.IngredienteSustitutoId)
            .IsRequired();

        entity.Property(sustituto => sustituto.FactorEquivalencia)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        entity.Property(sustituto => sustituto.Notas)
            .HasMaxLength(250);

        entity.HasOne(sustituto => sustituto.IngredienteOriginal)
            .WithMany()
            .HasForeignKey(sustituto => sustituto.IngredienteOriginalId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(sustituto => sustituto.IngredienteSustituto)
            .WithMany()
            .HasForeignKey(sustituto => sustituto.IngredienteSustitutoId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(sustituto => sustituto.IngredienteOriginalId);

        entity.HasIndex(sustituto => new
        {
            sustituto.IngredienteOriginalId,
            sustituto.IngredienteSustitutoId
        }).IsUnique();
    }
}
