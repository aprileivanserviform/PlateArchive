using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssociazioneMacchinePiastre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltezzaMassimaFoglioMm",
                table: "MacchineStandard");

            migrationBuilder.DropColumn(
                name: "AltezzaMinimaFoglioMm",
                table: "MacchineStandard");

            migrationBuilder.DropColumn(
                name: "LarghezzaMassimaFoglioMm",
                table: "MacchineStandard");

            migrationBuilder.DropColumn(
                name: "LarghezzaMinimaFoglioMm",
                table: "MacchineStandard");

            migrationBuilder.DropColumn(
                name: "Matricola",
                table: "ClientiMacchine");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AltezzaMassimaFoglioMm",
                table: "MacchineStandard",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AltezzaMinimaFoglioMm",
                table: "MacchineStandard",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LarghezzaMassimaFoglioMm",
                table: "MacchineStandard",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LarghezzaMinimaFoglioMm",
                table: "MacchineStandard",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Matricola",
                table: "ClientiMacchine",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
