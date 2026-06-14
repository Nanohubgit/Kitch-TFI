using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class RecetaConfiguration : IEntityTypeConfiguration<Receta>
    {
        public void Configure(EntityTypeBuilder<Receta> entity)
        {
            entity.ToTable("Receta");

            entity.HasKey(receta => receta.Id);

            entity.Property(receta => receta.Titulo)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(receta => receta.CaloriasEstimadas)
                .IsRequired();

            entity.Property(receta => receta.Descripcion)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(receta => receta.TiempoPreparacionMinutos)
                .IsRequired();

            entity.Property(receta => receta.Porciones)
                .IsRequired();

            entity.Property(receta => receta.Dificultad)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);
        }
    }
}