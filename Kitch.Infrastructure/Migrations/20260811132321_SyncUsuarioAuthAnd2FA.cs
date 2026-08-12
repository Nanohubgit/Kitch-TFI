using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncUsuarioAuthAnd2FA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreUsuario",
                table: "Usuario",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Usuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "Usuario",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferenciaDietetica",
                table: "Usuario",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Ninguna");

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorCode",
                table: "Usuario",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorCodeExpiresAt",
                table: "Usuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_NombreUsuario",
                table: "Usuario",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PasswordResetTokenHash",
                table: "Usuario",
                column: "PasswordResetTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuario_NombreUsuario",
                table: "Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_PasswordResetTokenHash",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "NombreUsuario",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "PreferenciaDietetica",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "TwoFactorCode",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "TwoFactorCodeExpiresAt",
                table: "Usuario");
        }
    }
}
