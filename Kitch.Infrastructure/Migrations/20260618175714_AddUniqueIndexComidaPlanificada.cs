using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexComidaPlanificada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComidaPlanificada_UsuarioId_FechaAsignada_Turno",
                table: "ComidaPlanificada");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaAsignada",
                table: "ComidaPlanificada",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_ComidaPlanificada_UsuarioId_FechaAsignada_Turno",
                table: "ComidaPlanificada",
                columns: new[] { "UsuarioId", "FechaAsignada", "Turno" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComidaPlanificada_UsuarioId_FechaAsignada_Turno",
                table: "ComidaPlanificada");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaAsignada",
                table: "ComidaPlanificada",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.CreateIndex(
                name: "IX_ComidaPlanificada_UsuarioId_FechaAsignada_Turno",
                table: "ComidaPlanificada",
                columns: new[] { "UsuarioId", "FechaAsignada", "Turno" });
        }
    }
}
