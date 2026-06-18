using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSustitutoIngrediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SustitutoIngrediente");
        }
    }
}
