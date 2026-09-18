using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioCuotaIaYStockNoNegativo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCuotaIaUtc",
                table: "Usuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeticionesIaDelDia",
                table: "Usuario",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UltimaRecetaIaJson",
                table: "Usuario",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE StockUsuario SET Cantidad = 0 WHERE Cantidad < 0;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockUsuario_CantidadNoNegativa",
                table: "StockUsuario",
                sql: "[Cantidad] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockUsuario_CantidadNoNegativa",
                table: "StockUsuario");

            migrationBuilder.DropColumn(
                name: "FechaCuotaIaUtc",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "PeticionesIaDelDia",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "UltimaRecetaIaJson",
                table: "Usuario");
        }
    }
}
