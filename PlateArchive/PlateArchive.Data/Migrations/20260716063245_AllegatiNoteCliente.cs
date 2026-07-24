using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllegatiNoteCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllegatiClienti",
                columns: table => new
                {
                    IdAllegato = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    NomeFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PercorsoFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DimensioneBytes = table.Column<long>(type: "bigint", nullable: false),
                    DataCaricamento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllegatiClienti", x => x.IdAllegato);
                    table.ForeignKey(
                        name: "FK_AllegatiClienti_Clienti_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteTecnicheClienti",
                columns: table => new
                {
                    IdNota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    Titolo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Testo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataModifica = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteTecnicheClienti", x => x.IdNota);
                    table.ForeignKey(
                        name: "FK_NoteTecnicheClienti_Clienti_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiClienti_IdCliente",
                table: "AllegatiClienti",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_NoteTecnicheClienti_IdCliente",
                table: "NoteTecnicheClienti",
                column: "IdCliente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllegatiClienti");

            migrationBuilder.DropTable(
                name: "NoteTecnicheClienti");
        }
    }
}
