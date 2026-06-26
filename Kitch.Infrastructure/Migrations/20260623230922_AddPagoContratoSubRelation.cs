using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoContratoSubRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Contrasena",
                table: "Usuario",
                newName: "PasswordHash");

            migrationBuilder.AddColumn<int>(
                name: "ContratoSubId",
                table: "Pago",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Pago_ContratoSubId",
                table: "Pago",
                column: "ContratoSubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pago_ContratoSub_ContratoSubId",
                table: "Pago",
                column: "ContratoSubId",
                principalTable: "ContratoSub",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pago_ContratoSub_ContratoSubId",
                table: "Pago");

            migrationBuilder.DropIndex(
                name: "IX_Pago_ContratoSubId",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "ContratoSubId",
                table: "Pago");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Usuario",
                newName: "Contrasena");
        }
    }
}
