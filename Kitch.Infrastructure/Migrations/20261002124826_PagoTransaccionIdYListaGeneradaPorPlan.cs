using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    public partial class PagoTransaccionIdYListaGeneradaPorPlan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransaccionId",
                table: "Pago",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GeneradoPorPlan",
                table: "ItemListaCompra",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_TransaccionId",
                table: "Pago",
                column: "TransaccionId",
                unique: true,
                filter: "[TransaccionId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pago_TransaccionId",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "TransaccionId",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "GeneradoPorPlan",
                table: "ItemListaCompra");
        }
    }
}
