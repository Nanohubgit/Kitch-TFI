using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredienteSustitutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngredienteSustituto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IngredienteId = table.Column<int>(type: "int", nullable: false),
                    SustitutoId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredienteSustituto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredienteSustituto_Ingrediente_IngredienteId",
                        column: x => x.IngredienteId,
                        principalTable: "Ingrediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredienteSustituto_Ingrediente_SustitutoId",
                        column: x => x.SustitutoId,
                        principalTable: "Ingrediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngredienteSustituto_IngredienteId",
                table: "IngredienteSustituto",
                column: "IngredienteId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredienteSustituto_IngredienteId_SustitutoId",
                table: "IngredienteSustituto",
                columns: new[] { "IngredienteId", "SustitutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngredienteSustituto_SustitutoId",
                table: "IngredienteSustituto",
                column: "SustitutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredienteSustituto");
        }
    }
}
