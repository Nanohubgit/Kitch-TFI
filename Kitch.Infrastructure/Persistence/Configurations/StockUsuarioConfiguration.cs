using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class StockUsuarioConfiguration : IEntityTypeConfiguration<StockUsuario>
    {
        public void Configure(EntityTypeBuilder<StockUsuario> entity)
        {
         entity.ToTable("StockUsuario");

            entity.HasKey(stockUsuario => stockUsuario.Id);

            entity.Property(stockUsuario => stockUsuario.UsuarioId)
                .IsRequired();

            entity.Property(stockUsuario => stockUsuario.IngredienteId)
                .IsRequired();

            entity.Property(stockUsuario => stockUsuario.Cantidad)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(stockUsuario => stockUsuario.UnidadMedida)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(stockUsuario => stockUsuario.Usuario)
                .WithMany()
                .HasForeignKey(stockUsuario => stockUsuario.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(stockUsuario => stockUsuario.Ingrediente)
                .WithMany()
                .HasForeignKey(stockUsuario => stockUsuario.IngredienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(stockUsuario => stockUsuario.UsuarioId);
            entity.HasIndex(stockUsuario => stockUsuario.IngredienteId);
            entity.HasIndex(stockUsuario => new
            {
                stockUsuario.UsuarioId,
                stockUsuario.IngredienteId
            }).IsUnique();
        }
    }
}