using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingrediente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingrediente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Receta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CaloriasEstimadas = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TiempoPreparacionMinutos = table.Column<int>(type: "int", nullable: false),
                    Porciones = table.Column<int>(type: "int", nullable: false),
                    Dificultad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SustitutoIngrediente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IngredienteOriginalId = table.Column<int>(type: "int", nullable: false),
                    IngredienteSustitutoId = table.Column<int>(type: "int", nullable: false),
                    FactorEquivalencia = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SustitutoIngrediente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SustitutoIngrediente_Ingrediente_IngredienteOriginalId",
                        column: x => x.IngredienteOriginalId,
                        principalTable: "Ingrediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SustitutoIngrediente_Ingrediente_IngredienteSustitutoId",
                        column: x => x.IngredienteSustitutoId,
                        principalTable: "Ingrediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngredienteReceta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecetaId = table.Column<int>(type: "int", nullable: false),
                    IngredienteId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredienteReceta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredienteReceta_Ingrediente_IngredienteId",
                        column: x => x.IngredienteId,
                        principalTable: "Ingrediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredienteReceta_Receta_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Receta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreparacionReceta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecetaId = table.Column<int>(type: "int", nullable: false),
                    NumeroPaso = table.Column<int>(type: "int", nullable: false),
                    DescripcionPaso = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparacionReceta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreparacionReceta_Receta_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Receta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComidaPlanificada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaAsignada = table.Column<DateTime>(type: "date", nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    RecetaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComidaPlanificada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComidaPlanificada_Receta_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Receta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComidaPlanificada_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemListaCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreArticulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CantidadFaltante = table.Column<float>(type: "real", nullable: false),
                    EstaComprado = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemListaCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemListaCompra_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecetaFavorita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    RecetaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecetaFavorita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecetaFavorita_Receta_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Receta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecetaFavorita_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    IngredienteId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaCaducidad = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockUsuario_Ingrediente_IngredienteId",
                        column: x => x.IngredienteId,
                        principalTable: "Ingrediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockUsuario_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Suscripcion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suscripcion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suscripcion_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContratoSub",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    SuscripcionId = table.Column<int>(type: "int", nullable: false),
                    FechaContratacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratoSub", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratoSub_Suscripcion_SuscripcionId",
                        column: x => x.SuscripcionId,
                        principalTable: "Suscripcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratoSub_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    ContratoSubId = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstadoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pago_ContratoSub_ContratoSubId",
                        column: x => x.ContratoSubId,
                        principalTable: "ContratoSub",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pago_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComidaPlanificada_RecetaId",
                table: "ComidaPlanificada",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComidaPlanificada_UsuarioId",
                table: "ComidaPlanificada",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ComidaPlanificada_UsuarioId_FechaAsignada_Turno",
                table: "ComidaPlanificada",
                columns: new[] { "UsuarioId", "FechaAsignada", "Turno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContratoSub_SuscripcionId",
                table: "ContratoSub",
                column: "SuscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoSub_UsuarioId",
                table: "ContratoSub",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingrediente_Nombre",
                table: "Ingrediente",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngredienteReceta_IngredienteId",
                table: "IngredienteReceta",
                column: "IngredienteId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredienteReceta_RecetaId",
                table: "IngredienteReceta",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredienteReceta_RecetaId_IngredienteId",
                table: "IngredienteReceta",
                columns: new[] { "RecetaId", "IngredienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemListaCompra_UsuarioId",
                table: "ItemListaCompra",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_ContratoSubId",
                table: "Pago",
                column: "ContratoSubId");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_FechaPago",
                table: "Pago",
                column: "FechaPago");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_UsuarioId",
                table: "Pago",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_UsuarioId_FechaPago",
                table: "Pago",
                columns: new[] { "UsuarioId", "FechaPago" });

            migrationBuilder.CreateIndex(
                name: "IX_PreparacionReceta_RecetaId",
                table: "PreparacionReceta",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_PreparacionReceta_RecetaId_NumeroPaso",
                table: "PreparacionReceta",
                columns: new[] { "RecetaId", "NumeroPaso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecetaFavorita_RecetaId",
                table: "RecetaFavorita",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaFavorita_UsuarioId",
                table: "RecetaFavorita",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaFavorita_UsuarioId_RecetaId",
                table: "RecetaFavorita",
                columns: new[] { "UsuarioId", "RecetaId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockUsuario_IngredienteId",
                table: "StockUsuario",
                column: "IngredienteId");

            migrationBuilder.CreateIndex(
                name: "IX_StockUsuario_UsuarioId",
                table: "StockUsuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_StockUsuario_UsuarioId_IngredienteId",
                table: "StockUsuario",
                columns: new[] { "UsuarioId", "IngredienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suscripcion_UsuarioId",
                table: "Suscripcion",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SustitutoIngrediente_IngredienteOriginalId",
                table: "SustitutoIngrediente",
                column: "IngredienteOriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_SustitutoIngrediente_IngredienteOriginalId_IngredienteSustitutoId",
                table: "SustitutoIngrediente",
                columns: new[] { "IngredienteOriginalId", "IngredienteSustitutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SustitutoIngrediente_IngredienteSustitutoId",
                table: "SustitutoIngrediente",
                column: "IngredienteSustitutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComidaPlanificada");

            migrationBuilder.DropTable(
                name: "IngredienteReceta");

            migrationBuilder.DropTable(
                name: "ItemListaCompra");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "PreparacionReceta");

            migrationBuilder.DropTable(
                name: "RecetaFavorita");

            migrationBuilder.DropTable(
                name: "StockUsuario");

            migrationBuilder.DropTable(
                name: "SustitutoIngrediente");

            migrationBuilder.DropTable(
                name: "ContratoSub");

            migrationBuilder.DropTable(
                name: "Receta");

            migrationBuilder.DropTable(
                name: "Ingrediente");

            migrationBuilder.DropTable(
                name: "Suscripcion");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
