using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriePiastre",
                columns: table => new
                {
                    IdCategoriaPiastra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codice      = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ordine      = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriePiastre", x => x.IdCategoriaPiastra);
                });

            migrationBuilder.CreateTable(
                name: "Clienti",
                columns: table => new
                {
                    IdCliente               = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodiceClienteGestionale = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RagioneSociale          = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note                    = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clienti", x => x.IdCliente);
                });

            migrationBuilder.CreateTable(
                name: "MacchineStandard",
                columns: table => new
                {
                    IdMacchinaStandard = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodiceMacchina     = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NomeMacchina       = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Famiglia           = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Formato            = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Versione           = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Produttore         = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attiva             = table.Column<bool>(type: "bit", nullable: false),
                    Note               = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacchineStandard", x => x.IdMacchinaStandard);
                });

            migrationBuilder.CreateTable(
                name: "Piastre",
                columns: table => new
                {
                    IdPiastra                = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodicePiastra            = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CodiceArticoloGestionale = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Descrizione              = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stato                    = table.Column<int>(type: "int", nullable: false),
                    IdCategoriaPiastra       = table.Column<int>(type: "int", nullable: true),
                    IsEliminata              = table.Column<bool>(type: "bit", nullable: false),
                    LarghezzaMm              = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AltezzaMm                = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SpessoreMm               = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Durezza                  = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Peso                     = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Note                     = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataCreazione            = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataUltimaModifica       = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Piastre", x => x.IdPiastra);
                    table.ForeignKey(
                        name: "FK_Piastre_CategoriePiastre_IdCategoriaPiastra",
                        column: x => x.IdCategoriaPiastra,
                        principalTable: "CategoriePiastre",
                        principalColumn: "IdCategoriaPiastra",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ClientiMacchine",
                columns: table => new
                {
                    IdClienteMacchina    = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCliente            = table.Column<int>(type: "int", nullable: false),
                    IdMacchinaStandard   = table.Column<int>(type: "int", nullable: false),
                    Matricola            = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodiceInternoCliente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataAssociazione     = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attiva               = table.Column<bool>(type: "bit", nullable: false),
                    Note                 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiMacchine", x => x.IdClienteMacchina);
                    table.ForeignKey(
                        name: "FK_ClientiMacchine_Clienti_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientiMacchine_MacchineStandard_IdMacchinaStandard",
                        column: x => x.IdMacchinaStandard,
                        principalTable: "MacchineStandard",
                        principalColumn: "IdMacchinaStandard",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Disegni",
                columns: table => new
                {
                    IdDisegno              = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPiastra              = table.Column<int>(type: "int", nullable: false),
                    CodiceDisegno          = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NomeFile               = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PercorsoFile           = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VaultId                = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Revisione              = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Formato                = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stato                  = table.Column<int>(type: "int", nullable: false),
                    DataUltimaModificaFile = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note                   = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disegni", x => x.IdDisegno);
                    table.ForeignKey(
                        name: "FK_Disegni_Piastre_IdPiastra",
                        column: x => x.IdPiastra,
                        principalTable: "Piastre",
                        principalColumn: "IdPiastra",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PiastreMacchineCompatibili",
                columns: table => new
                {
                    IdCompatibilita    = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPiastra          = table.Column<int>(type: "int", nullable: false),
                    IdMacchinaStandard = table.Column<int>(type: "int", nullable: false),
                    FonteDato          = table.Column<int>(type: "int", nullable: true),
                    DataVerifica       = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UtenteVerifica     = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attiva             = table.Column<bool>(type: "bit", nullable: false),
                    Note               = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiastreMacchineCompatibili", x => x.IdCompatibilita);
                    table.ForeignKey(
                        name: "FK_PiastreMacchineCompatibili_MacchineStandard_IdMacchinaStandard",
                        column: x => x.IdMacchinaStandard,
                        principalTable: "MacchineStandard",
                        principalColumn: "IdMacchinaStandard",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PiastreMacchineCompatibili_Piastre_IdPiastra",
                        column: x => x.IdPiastra,
                        principalTable: "Piastre",
                        principalColumn: "IdPiastra",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientiPiastre",
                columns: table => new
                {
                    IdClientePiastra  = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCliente         = table.Column<int>(type: "int", nullable: false),
                    IdPiastra         = table.Column<int>(type: "int", nullable: false),
                    IdClienteMacchina = table.Column<int>(type: "int", nullable: true),
                    DataAssociazione  = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stato             = table.Column<int>(type: "int", nullable: false),
                    Note              = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiPiastre", x => x.IdClientePiastra);
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_Clienti_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_ClientiMacchine_IdClienteMacchina",
                        column: x => x.IdClienteMacchina,
                        principalTable: "ClientiMacchine",
                        principalColumn: "IdClienteMacchina",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_Piastre_IdPiastra",
                        column: x => x.IdPiastra,
                        principalTable: "Piastre",
                        principalColumn: "IdPiastra",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indici
            migrationBuilder.CreateIndex(
                name: "IX_CategoriePiastre_Codice",
                table: "CategoriePiastre",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clienti_CodiceClienteGestionale",
                table: "Clienti",
                column: "CodiceClienteGestionale",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientiMacchine_IdCliente",
                table: "ClientiMacchine",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiMacchine_IdMacchinaStandard",
                table: "ClientiMacchine",
                column: "IdMacchinaStandard");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdCliente",
                table: "ClientiPiastre",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdPiastra",
                table: "ClientiPiastre",
                column: "IdPiastra");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdClienteMacchina",
                table: "ClientiPiastre",
                column: "IdClienteMacchina");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdCliente_IdPiastra",
                table: "ClientiPiastre",
                columns: new[] { "IdCliente", "IdPiastra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disegni_IdPiastra",
                table: "Disegni",
                column: "IdPiastra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MacchineStandard_CodiceMacchina",
                table: "MacchineStandard",
                column: "CodiceMacchina",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_CodiceArticoloGestionale",
                table: "Piastre",
                column: "CodiceArticoloGestionale",
                unique: true,
                filter: "[CodiceArticoloGestionale] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_CodicePiastra",
                table: "Piastre",
                column: "CodicePiastra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_IdCategoriaPiastra",
                table: "Piastre",
                column: "IdCategoriaPiastra");

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_IdMacchinaStandard",
                table: "PiastreMacchineCompatibili",
                column: "IdMacchinaStandard");

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_IdPiastra",
                table: "PiastreMacchineCompatibili",
                column: "IdPiastra");

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_IdPiastra_IdMacchinaStandard",
                table: "PiastreMacchineCompatibili",
                columns: new[] { "IdPiastra", "IdMacchinaStandard" },
                unique: true);

            // Dati seed per le categorie
            migrationBuilder.InsertData(
                table: "CategoriePiastre",
                columns: new[] { "Codice", "Descrizione", "Ordine" },
                values: new object[,]
                {
                    { "STD", "Standard", 1 },
                    { "SPE", "Speciale", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ClientiPiastre");
            migrationBuilder.DropTable(name: "Disegni");
            migrationBuilder.DropTable(name: "PiastreMacchineCompatibili");
            migrationBuilder.DropTable(name: "ClientiMacchine");
            migrationBuilder.DropTable(name: "Piastre");
            migrationBuilder.DropTable(name: "CategoriePiastre");
            migrationBuilder.DropTable(name: "Clienti");
            migrationBuilder.DropTable(name: "MacchineStandard");
        }
    }
}
