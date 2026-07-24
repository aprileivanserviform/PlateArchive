using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class RimuoviPesoPiastra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Peso",
                table: "Piastre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Peso",
                table: "Piastre",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
