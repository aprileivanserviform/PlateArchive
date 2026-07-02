using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteAttivoGestionale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue true (non il false generato da EF): i clienti già presenti
            // devono partire come attivi — sarà la prima sincronizzazione a marcare gli annullati.
            migrationBuilder.AddColumn<bool>(
                name: "AttivoGestionale",
                table: "Clienti",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttivoGestionale",
                table: "Clienti");
        }
    }
}
