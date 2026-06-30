using System;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    [DbContext(typeof(KitchDbContext))]
    [Migration("20260627235749_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Kitch.Domain.Entities.ComidaPlanificada", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("FechaAsignada")
                        .HasColumnType("date");

                    b.Property<int>("RecetaId")
                        .HasColumnType("int");

                    b.Property<string>("Turno")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RecetaId");

                    b.HasIndex("UsuarioId");

                    b.HasIndex("UsuarioId", "FechaAsignada", "Turno")
                        .IsUnique();

                    b.ToTable("ComidaPlanificada", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.ContratoSub", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("FechaContratacion")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("FechaFin")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("FechaInicio")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SuscripcionId")
                        .HasColumnType("int");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SuscripcionId");

                    b.HasIndex("UsuarioId");

                    b.ToTable("ContratoSub", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Ingrediente", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Categoria")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("Nombre")
                        .IsUnique();

                    b.ToTable("Ingrediente", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.IngredienteReceta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasPrecision(10, 2)
                        .HasColumnType("decimal(10,2)");

                    b.Property<int>("IngredienteId")
                        .HasColumnType("int");

                    b.Property<int>("RecetaId")
                        .HasColumnType("int");

                    b.Property<string>("UnidadMedida")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("IngredienteId");

                    b.HasIndex("RecetaId");

                    b.HasIndex("RecetaId", "IngredienteId")
                        .IsUnique();

                    b.ToTable("IngredienteReceta", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.ItemListaCompra", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<float>("CantidadFaltante")
                        .HasColumnType("real");

                    b.Property<bool>("EstaComprado")
                        .HasColumnType("bit");

                    b.Property<string>("NombreArticulo")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UsuarioId");

                    b.ToTable("ItemListaCompra", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Pago", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ContratoSubId")
                        .HasColumnType("int");

                    b.Property<string>("EstadoPago")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("FechaPago")
                        .HasColumnType("datetime2");

                    b.Property<string>("MetodoPago")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<decimal>("Monto")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ContratoSubId");

                    b.HasIndex("FechaPago");

                    b.HasIndex("UsuarioId");

                    b.HasIndex("UsuarioId", "FechaPago");

                    b.ToTable("Pago", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.PreparacionReceta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("DescripcionPaso")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<int>("NumeroPaso")
                        .HasColumnType("int");

                    b.Property<int>("RecetaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RecetaId");

                    b.HasIndex("RecetaId", "NumeroPaso")
                        .IsUnique();

                    b.ToTable("PreparacionReceta", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Receta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CaloriasEstimadas")
                        .HasColumnType("int");

                    b.Property<string>("Descripcion")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("Dificultad")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("Porciones")
                        .HasColumnType("int");

                    b.Property<int>("TiempoPreparacionMinutos")
                        .HasColumnType("int");

                    b.Property<string>("Titulo")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    b.ToTable("Receta", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.RecetaFavorita", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("RecetaId")
                        .HasColumnType("int");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RecetaId");

                    b.HasIndex("UsuarioId");

                    b.HasIndex("UsuarioId", "RecetaId");

                    b.ToTable("RecetaFavorita", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.StockUsuario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(10,2)");

                    b.Property<DateTime?>("FechaCaducidad")
                        .HasColumnType("date");

                    b.Property<int>("IngredienteId")
                        .HasColumnType("int");

                    b.Property<string>("UnidadMedida")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("IngredienteId");

                    b.HasIndex("UsuarioId");

                    b.HasIndex("UsuarioId", "IngredienteId")
                        .IsUnique();

                    b.ToTable("StockUsuario", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Suscripcion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("FechaFin")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("FechaInicio")
                        .HasColumnType("datetime2");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UsuarioId");

                    b.ToTable("Suscripcion", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.SustitutoIngrediente", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("FactorEquivalencia")
                        .HasColumnType("decimal(10,2)");

                    b.Property<int>("IngredienteOriginalId")
                        .HasColumnType("int");

                    b.Property<int>("IngredienteSustitutoId")
                        .HasColumnType("int");

                    b.Property<string>("Notas")
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.HasKey("Id");

                    b.HasIndex("IngredienteOriginalId");

                    b.HasIndex("IngredienteSustitutoId");

                    b.HasIndex("IngredienteOriginalId", "IngredienteSustitutoId")
                        .IsUnique();

                    b.ToTable("SustitutoIngrediente", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Usuario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("bit");

                    b.Property<string>("Apellido")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("Rol")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Usuario", (string)null);
                });

            modelBuilder.Entity("Kitch.Domain.Entities.ComidaPlanificada", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Receta", "Receta")
                        .WithMany()
                        .HasForeignKey("RecetaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Kitch.Domain.Entities.Usuario", "Usuario")
                        .WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Receta");

                    b.Navigation("Usuario");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.ContratoSub", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Suscripcion", "Suscripcion")
                        .WithMany()
                        .HasForeignKey("SuscripcionId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Kitch.Domain.Entities.Usuario", "Usuario")
                        .WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Suscripcion");

                    b.Navigation("Usuario");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.IngredienteReceta", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Ingrediente", "Ingrediente")
                        .WithMany("IngredientesReceta")
                        .HasForeignKey("IngredienteId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Kitch.Domain.Entities.Receta", "Receta")
                        .WithMany("IngredientesReceta")
                        .HasForeignKey("RecetaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Ingrediente");

                    b.Navigation("Receta");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.ItemListaCompra", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Usuario", "Usuario")
                        .WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Usuario");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Pago", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.ContratoSub", "ContratoSub")
                        .WithMany("Pagos")
                        .HasForeignKey("ContratoSubId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Kitch.Domain.Entities.Usuario", "Usuario")
                        .WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ContratoSub");

                    b.Navigation("Usuario");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.PreparacionReceta", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Receta", "Receta")
                        .WithMany("Preparaciones")
                        .HasForeignKey("RecetaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Receta");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.RecetaFavorita", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Receta", "Receta")
                        .WithMany()
                        .HasForeignKey("RecetaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Kitch.Domain.Entities.Usuario", "Usuario")
                        .WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Receta");

                    b.Navigation("Usuario");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.StockUsuario", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Ingrediente", "Ingrediente")
                        .WithMany()
                        .HasForeignKey("IngredienteId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Kitch.Domain.Entities.Usuario", "Usuario")
                        .WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Ingrediente");

                    b.Navigation("Usuario");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Suscripcion", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Usuario", "Usuario")
                        .WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Usuario");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.SustitutoIngrediente", b =>
                {
                    b.HasOne("Kitch.Domain.Entities.Ingrediente", "IngredienteOriginal")
                        .WithMany()
                        .HasForeignKey("IngredienteOriginalId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Kitch.Domain.Entities.Ingrediente", "IngredienteSustituto")
                        .WithMany()
                        .HasForeignKey("IngredienteSustitutoId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("IngredienteOriginal");

                    b.Navigation("IngredienteSustituto");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.ContratoSub", b =>
                {
                    b.Navigation("Pagos");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Ingrediente", b =>
                {
                    b.Navigation("IngredientesReceta");
                });

            modelBuilder.Entity("Kitch.Domain.Entities.Receta", b =>
                {
                    b.Navigation("IngredientesReceta");

                    b.Navigation("Preparaciones");
                });
#pragma warning restore 612, 618
        }
    }
}
