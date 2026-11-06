using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    public partial class RecetaPropietario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Receta",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receta_UsuarioId",
                table: "Receta",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Receta_Usuario_UsuarioId",
                table: "Receta",
                column: "UsuarioId",
                principalTable: "Usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                "UPDATE r SET r.UsuarioId = f.UsuarioId " +
                "FROM Receta r " +
                "CROSS APPLY (SELECT MIN(UsuarioId) AS UsuarioId, COUNT(DISTINCT UsuarioId) AS Cantidad " +
                "FROM RecetaFavorita WHERE RecetaId = r.Id) f " +
                "WHERE r.UsuarioId IS NULL AND f.Cantidad = 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receta_Usuario_UsuarioId",
                table: "Receta");

            migrationBuilder.DropIndex(
                name: "IX_Receta_UsuarioId",
                table: "Receta");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Receta");
        }
    }
}
