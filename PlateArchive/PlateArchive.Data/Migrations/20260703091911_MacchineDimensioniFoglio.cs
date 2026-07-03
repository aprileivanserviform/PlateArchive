using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class MacchineDimensioniFoglio : Migration
    {
        // Le macchine di fustellatura non hanno una singola dimensione fisica, ma un
        // formato foglio lavorabile compreso tra un minimo e un massimo (L × A).
        // Le vecchie colonne LarghezzaMm/AltezzaMm vengono rinominate come dimensioni
        // MASSIME del foglio (interpretazione più coerente dei dati esistenti); le
        // dimensioni minime vengono aggiunte come nuove colonne nullable.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LarghezzaMm",
                table: "MacchineStandard",
                newName: "LarghezzaMassimaFoglioMm");

            migrationBuilder.RenameColumn(
                name: "AltezzaMm",
                table: "MacchineStandard",
                newName: "AltezzaMassimaFoglioMm");

            migrationBuilder.AddColumn<decimal>(
                name: "LarghezzaMinimaFoglioMm",
                table: "MacchineStandard",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AltezzaMinimaFoglioMm",
                table: "MacchineStandard",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LarghezzaMinimaFoglioMm",
                table: "MacchineStandard");

            migrationBuilder.DropColumn(
                name: "AltezzaMinimaFoglioMm",
                table: "MacchineStandard");

            migrationBuilder.RenameColumn(
                name: "LarghezzaMassimaFoglioMm",
                table: "MacchineStandard",
                newName: "LarghezzaMm");

            migrationBuilder.RenameColumn(
                name: "AltezzaMassimaFoglioMm",
                table: "MacchineStandard",
                newName: "AltezzaMm");
        }
    }
}
