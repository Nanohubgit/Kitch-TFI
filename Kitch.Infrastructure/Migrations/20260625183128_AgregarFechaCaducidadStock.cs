using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFechaCaducidadStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCaducidad",
                table: "StockUsuario",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCaducidad",
                table: "StockUsuario");
        }
    }
}
