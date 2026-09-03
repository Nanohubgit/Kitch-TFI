using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ListaCompraIngredienteYUnidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IngredienteId",
                table: "ItemListaCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnidadMedida",
                table: "ItemListaCompra",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ItemListaCompra_IngredienteId",
                table: "ItemListaCompra",
                column: "IngredienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemListaCompra_Ingrediente_IngredienteId",
                table: "ItemListaCompra",
                column: "IngredienteId",
                principalTable: "Ingrediente",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemListaCompra_Ingrediente_IngredienteId",
                table: "ItemListaCompra");

            migrationBuilder.DropIndex(
                name: "IX_ItemListaCompra_IngredienteId",
                table: "ItemListaCompra");

            migrationBuilder.DropColumn(
                name: "IngredienteId",
                table: "ItemListaCompra");

            migrationBuilder.DropColumn(
                name: "UnidadMedida",
                table: "ItemListaCompra");
        }
    }
}
