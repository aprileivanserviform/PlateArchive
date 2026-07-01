using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProduttoriFamiglieMacchine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FamiglieMacchine",
                columns: table => new
                {
                    IdFamiglia   = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeFamiglia = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsEliminata  = table.Column<bool>(type: "bit", nullable: false),
                    Note         = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamiglieMacchine", x => x.IdFamiglia);
                });

            migrationBuilder.CreateTable(
                name: "ProduttoriMacchine",
                columns: table => new
                {
                    IdProduttore   = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeProduttore = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsEliminata    = table.Column<bool>(type: "bit", nullable: false),
                    Note           = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProduttoriMacchine", x => x.IdProduttore);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FamiglieMacchine_NomeFamiglia",
                table: "FamiglieMacchine",
                column: "NomeFamiglia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProduttoriMacchine_NomeProduttore",
                table: "ProduttoriMacchine",
                column: "NomeProduttore",
                unique: true);

            // Aggiunge le nuove colonne a MacchineStandard
            migrationBuilder.AddColumn<int>(
                name: "IdFamiglia",
                table: "MacchineStandard",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdProduttore",
                table: "MacchineStandard",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LarghezzaMm",
                table: "MacchineStandard",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AltezzaMm",
                table: "MacchineStandard",
                type: "decimal(18,2)",
                nullable: true);

            // Rimuove le vecchie colonne stringa
            migrationBuilder.DropColumn(name: "Famiglia",  table: "MacchineStandard");
            migrationBuilder.DropColumn(name: "Formato",   table: "MacchineStandard");
            migrationBuilder.DropColumn(name: "Produttore", table: "MacchineStandard");

            // FK verso le nuove tabelle lookup (NO ACTION lato DB — nulling gestito da EF ClientSetNull)
            migrationBuilder.AddForeignKey(
                name: "FK_MacchineStandard_FamiglieMacchine_IdFamiglia",
                table: "MacchineStandard",
                column: "IdFamiglia",
                principalTable: "FamiglieMacchine",
                principalColumn: "IdFamiglia",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_MacchineStandard_ProduttoriMacchine_IdProduttore",
                table: "MacchineStandard",
                column: "IdProduttore",
                principalTable: "ProduttoriMacchine",
                principalColumn: "IdProduttore",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.CreateIndex(
                name: "IX_MacchineStandard_IdFamiglia",
                table: "MacchineStandard",
                column: "IdFamiglia");

            migrationBuilder.CreateIndex(
                name: "IX_MacchineStandard_IdProduttore",
                table: "MacchineStandard",
                column: "IdProduttore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_MacchineStandard_FamiglieMacchine_IdFamiglia",     table: "MacchineStandard");
            migrationBuilder.DropForeignKey(name: "FK_MacchineStandard_ProduttoriMacchine_IdProduttore", table: "MacchineStandard");

            migrationBuilder.DropIndex(name: "IX_MacchineStandard_IdFamiglia",   table: "MacchineStandard");
            migrationBuilder.DropIndex(name: "IX_MacchineStandard_IdProduttore", table: "MacchineStandard");

            migrationBuilder.DropColumn(name: "IdFamiglia",   table: "MacchineStandard");
            migrationBuilder.DropColumn(name: "IdProduttore", table: "MacchineStandard");
            migrationBuilder.DropColumn(name: "LarghezzaMm",  table: "MacchineStandard");
            migrationBuilder.DropColumn(name: "AltezzaMm",    table: "MacchineStandard");

            migrationBuilder.AddColumn<string>(name: "Famiglia",   table: "MacchineStandard", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Formato",    table: "MacchineStandard", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Produttore", table: "MacchineStandard", type: "nvarchar(max)", nullable: true);

            migrationBuilder.DropIndex(name: "IX_FamiglieMacchine_NomeFamiglia",     table: "FamiglieMacchine");
            migrationBuilder.DropIndex(name: "IX_ProduttoriMacchine_NomeProduttore", table: "ProduttoriMacchine");

            migrationBuilder.DropTable(name: "FamiglieMacchine");
            migrationBuilder.DropTable(name: "ProduttoriMacchine");
        }
    }
}
