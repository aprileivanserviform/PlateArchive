using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class RimuoviCodiceArticoloGestionalePiastra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Piastre_CodiceArticoloGestionale",
                table: "Piastre");

            migrationBuilder.DropColumn(
                name: "CodiceArticoloGestionale",
                table: "Piastre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodiceArticoloGestionale",
                table: "Piastre",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_CodiceArticoloGestionale",
                table: "Piastre",
                column: "CodiceArticoloGestionale",
                unique: true,
                filter: "[CodiceArticoloGestionale] IS NOT NULL");
        }
    }
}
