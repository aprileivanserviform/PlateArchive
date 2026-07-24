using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class DurezzaStandard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Durezza",
                table: "Piastre");

            migrationBuilder.AddColumn<int>(
                name: "IdDurezza",
                table: "Piastre",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DurezzePiastre",
                columns: table => new
                {
                    IdDurezza = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Valore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEliminata = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DurezzePiastre", x => x.IdDurezza);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_IdDurezza",
                table: "Piastre",
                column: "IdDurezza");

            migrationBuilder.AddForeignKey(
                name: "FK_Piastre_DurezzePiastre_IdDurezza",
                table: "Piastre",
                column: "IdDurezza",
                principalTable: "DurezzePiastre",
                principalColumn: "IdDurezza",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Piastre_DurezzePiastre_IdDurezza",
                table: "Piastre");

            migrationBuilder.DropTable(
                name: "DurezzePiastre");

            migrationBuilder.DropIndex(
                name: "IX_Piastre_IdDurezza",
                table: "Piastre");

            migrationBuilder.DropColumn(
                name: "IdDurezza",
                table: "Piastre");

            migrationBuilder.AddColumn<decimal>(
                name: "Durezza",
                table: "Piastre",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
