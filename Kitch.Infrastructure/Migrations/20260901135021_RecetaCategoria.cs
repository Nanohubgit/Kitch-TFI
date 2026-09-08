using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kitch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecetaCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Receta",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "general");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Receta");
        }
    }
}
