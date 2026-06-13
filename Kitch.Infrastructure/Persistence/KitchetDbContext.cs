using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Persistence;

public class KitchenDbContext : DbContext
{
    public KitchenDbContext(DbContextOptions<KitchenDbContext> options)
        : base(options)
    {
    }

    public DbSet<ComidaPlanificada> ComidasPlanificadas => Set<ComidaPlanificada>();
    public DbSet<ItemListaCompra> ItemsListaCompra => Set<ItemListaCompra>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaFavorita> RecetasFavoritas => Set<RecetaFavorita>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(KitchenDbContext).Assembly);
        }
}
