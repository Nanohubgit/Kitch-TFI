using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class RecetaFavoritaConfiguration : IEntityTypeConfiguration<RecetaFavorita>
    {
        public void Configure(EntityTypeBuilder<RecetaFavorita> entity)
        {
            entity.ToTable("RecetaFavorita");

            entity.HasKey(recetaFavorita => recetaFavorita.Id);

            entity.Property(recetaFavorita => recetaFavorita.UsuarioId)
                .IsRequired();

            entity.Property(recetaFavorita => recetaFavorita.RecetaId)
                .IsRequired();

            entity.HasOne(recetaFavorita => recetaFavorita.Usuario)
                .WithMany()
                .HasForeignKey(recetaFavorita => recetaFavorita.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(recetaFavorita => recetaFavorita.Receta)
                .WithMany()
                .HasForeignKey(recetaFavorita => recetaFavorita.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(recetaFavorita => recetaFavorita.UsuarioId);
            entity.HasIndex(recetaFavorita => recetaFavorita.RecetaId);
            entity.HasIndex(recetaFavorita => new
            {
                recetaFavorita.UsuarioId,
                recetaFavorita.RecetaId
            });
        }
    }
}