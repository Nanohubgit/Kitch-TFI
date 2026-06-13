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
    public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();
    public DbSet<IngredienteReceta> IngredientesReceta => Set<IngredienteReceta>();
    public DbSet<ItemListaCompra> ItemsListaCompra => Set<ItemListaCompra>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<PreparacionReceta> PreparacionesReceta => Set<PreparacionReceta>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaFavorita> RecetasFavoritas => Set<RecetaFavorita>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");

            entity.HasKey(usuario => usuario.Id);

            entity.Property(usuario => usuario.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(usuario => usuario.Apellido)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(usuario => usuario.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(usuario => usuario.Contraseña)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(usuario => usuario.Activo)
                .IsRequired();

            entity.Property(usuario => usuario.Rol)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(usuario => usuario.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Receta>(entity =>
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
        });

        modelBuilder.Entity<ComidaPlanificada>(entity =>
        {
            entity.ToTable("ComidaPlanificada");

            entity.HasKey(comidaPlanificada => comidaPlanificada.Id);

            entity.Property(comidaPlanificada => comidaPlanificada.FechaAsignada)
                .IsRequired();

            entity.Property(comidaPlanificada => comidaPlanificada.Turno)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(comidaPlanificada => comidaPlanificada.UsuarioId)
                .IsRequired();

            entity.Property(comidaPlanificada => comidaPlanificada.RecetaId)
                .IsRequired();

            entity.HasOne(comidaPlanificada => comidaPlanificada.Usuario)
                .WithMany()
                .HasForeignKey(comidaPlanificada => comidaPlanificada.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(comidaPlanificada => comidaPlanificada.Receta)
                .WithMany()
                .HasForeignKey(comidaPlanificada => comidaPlanificada.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(comidaPlanificada => comidaPlanificada.UsuarioId);
            entity.HasIndex(comidaPlanificada => comidaPlanificada.RecetaId);
            entity.HasIndex(comidaPlanificada => new
            {
                comidaPlanificada.UsuarioId,
                comidaPlanificada.FechaAsignada,
                comidaPlanificada.Turno
            });
        });

        modelBuilder.Entity<Ingrediente>(entity =>
        {
            entity.ToTable("Ingrediente");

            entity.HasKey(ingrediente => ingrediente.Id);

            entity.Property(ingrediente => ingrediente.Nombre)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(ingrediente => ingrediente.Descripcion)
                .HasMaxLength(500);

            entity.HasIndex(ingrediente => ingrediente.Nombre)
                .IsUnique();
        });

        modelBuilder.Entity<IngredienteReceta>(entity =>
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
                .WithMany()
                .HasForeignKey(ingredienteReceta => ingredienteReceta.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ingredienteReceta => ingredienteReceta.Ingrediente)
                .WithMany()
                .HasForeignKey(ingredienteReceta => ingredienteReceta.IngredienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(ingredienteReceta => ingredienteReceta.RecetaId);
            entity.HasIndex(ingredienteReceta => ingredienteReceta.IngredienteId);
            entity.HasIndex(ingredienteReceta => new
            {
                ingredienteReceta.RecetaId,
                ingredienteReceta.IngredienteId
            }).IsUnique();
        });

        modelBuilder.Entity<ItemListaCompra>(entity =>
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

            entity.Property(itemListaCompra => itemListaCompra.UsuarioId)
                .IsRequired();

            entity.HasOne(itemListaCompra => itemListaCompra.Usuario)
                .WithMany()
                .HasForeignKey(itemListaCompra => itemListaCompra.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(itemListaCompra => itemListaCompra.UsuarioId);
        });

        modelBuilder.Entity<PreparacionReceta>(entity =>
        {
            entity.ToTable("PreparacionReceta");

            entity.HasKey(preparacionReceta => preparacionReceta.Id);

            entity.Property(preparacionReceta => preparacionReceta.RecetaId)
                .IsRequired();

            entity.Property(preparacionReceta => preparacionReceta.NumeroPaso)
                .IsRequired();

            entity.Property(preparacionReceta => preparacionReceta.DescripcionPaso)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasOne(preparacionReceta => preparacionReceta.Receta)
                .WithMany()
                .HasForeignKey(preparacionReceta => preparacionReceta.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(preparacionReceta => preparacionReceta.RecetaId);
            entity.HasIndex(preparacionReceta => new
            {
                preparacionReceta.RecetaId,
                preparacionReceta.NumeroPaso
            }).IsUnique();
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.ToTable("Pago");

            entity.HasKey(pago => pago.Id);

            entity.Property(pago => pago.UsuarioId)
                .IsRequired();

            entity.Property(pago => pago.FechaPago)
                .IsRequired();

            entity.Property(pago => pago.Monto)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(pago => pago.EstadoPago)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(pago => pago.MetodoPago)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(pago => pago.Usuario)
                .WithMany()
                .HasForeignKey(pago => pago.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pago => pago.UsuarioId);
        });

        modelBuilder.Entity<RecetaFavorita>(entity =>
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
            }).IsUnique();
        });
    }
}
