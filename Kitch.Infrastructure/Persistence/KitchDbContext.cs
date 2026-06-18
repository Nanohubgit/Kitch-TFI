using Kitch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Persistence;

public class KitchDbContext : DbContext
{
    public KitchDbContext(DbContextOptions<KitchDbContext> options)
        : base(options)
    {
    }

    public DbSet<ComidaPlanificada> ComidasPlanificadas => Set<ComidaPlanificada>();
    public DbSet<ContratoSub> ContratosSub => Set<ContratoSub>();
    public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();
    public DbSet<IngredienteReceta> IngredientesReceta => Set<IngredienteReceta>();
    public DbSet<ItemListaCompra> ItemsListaCompra => Set<ItemListaCompra>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<PreparacionReceta> PreparacionesReceta => Set<PreparacionReceta>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaFavorita> RecetasFavoritas => Set<RecetaFavorita>();
    public DbSet<StockUsuario> StockUsuarios => Set<StockUsuario>();
    public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();
    public DbSet<SustitutoIngrediente> SustitutosIngrediente => Set<SustitutoIngrediente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(KitchDbContext).Assembly);
        }
}
