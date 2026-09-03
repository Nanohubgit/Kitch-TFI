using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitch.Infrastructure.Persistence.Configurations
{
    public class ItemListaCompraConfiguration : IEntityTypeConfiguration<ItemListaCompra>
    {
        public void Configure(EntityTypeBuilder<ItemListaCompra> entity)
        {
            entity.ToTable("ItemListaCompra");

            entity.HasKey(itemListaCompra => itemListaCompra.Id);

            entity.Property(itemListaCompra => itemListaCompra.NombreArticulo)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(itemListaCompra => itemListaCompra.CantidadFaltante)
                .IsRequired();

            entity.Property(itemListaCompra => itemListaCompra.EstaComprado)
                .IsRequired();

            entity.Property(itemListaCompra => itemListaCompra.UnidadMedida)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(itemListaCompra => itemListaCompra.UsuarioId)
                .IsRequired();

            entity.HasOne(itemListaCompra => itemListaCompra.Usuario)
                .WithMany()
                .HasForeignKey(itemListaCompra => itemListaCompra.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(itemListaCompra => itemListaCompra.Ingrediente)
                .WithMany()
                .HasForeignKey(itemListaCompra => itemListaCompra.IngredienteId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(itemListaCompra => itemListaCompra.UsuarioId);
            entity.HasIndex(itemListaCompra => itemListaCompra.IngredienteId);
        }
     }
}
