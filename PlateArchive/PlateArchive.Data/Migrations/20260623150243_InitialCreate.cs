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
                name: "Clienti",
                columns: table => new
                {
                    IdCliente = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodiceClienteGestionale = table.Column<string>(type: "TEXT", nullable: false),
                    RagioneSociale = table.Column<string>(type: "TEXT", nullable: false),
                    PartitaIVA = table.Column<string>(type: "TEXT", nullable: true),
                    CodiceFiscale = table.Column<string>(type: "TEXT", nullable: true),
                    StatoCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    DataUltimaSincronizzazione = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clienti", x => x.IdCliente);
                });

            migrationBuilder.CreateTable(
                name: "MacchineStandard",
                columns: table => new
                {
                    IdMacchinaStandard = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodiceMacchina = table.Column<string>(type: "TEXT", nullable: false),
                    NomeMacchina = table.Column<string>(type: "TEXT", nullable: false),
                    Famiglia = table.Column<string>(type: "TEXT", nullable: true),
                    Formato = table.Column<string>(type: "TEXT", nullable: true),
                    Versione = table.Column<string>(type: "TEXT", nullable: true),
                    Produttore = table.Column<string>(type: "TEXT", nullable: true),
                    Attiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacchineStandard", x => x.IdMacchinaStandard);
                });

            migrationBuilder.CreateTable(
                name: "Piastre",
                columns: table => new
                {
                    IdPiastra = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodicePiastra = table.Column<string>(type: "TEXT", nullable: false),
                    CodiceArticoloGestionale = table.Column<string>(type: "TEXT", nullable: true),
                    Descrizione = table.Column<string>(type: "TEXT", nullable: true),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataUltimaModifica = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Piastre", x => x.IdPiastra);
                });

            migrationBuilder.CreateTable(
                name: "ClientiMacchine",
                columns: table => new
                {
                    IdClienteMacchina = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    IdMacchinaStandard = table.Column<int>(type: "INTEGER", nullable: false),
                    Matricola = table.Column<string>(type: "TEXT", nullable: true),
                    CodiceInternoCliente = table.Column<string>(type: "TEXT", nullable: true),
                    DataAssociazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Attiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    ClienteIdCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    MacchinaStandardIdMacchinaStandard = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiMacchine", x => x.IdClienteMacchina);
                    table.ForeignKey(
                        name: "FK_ClientiMacchine_Clienti_ClienteIdCliente",
                        column: x => x.ClienteIdCliente,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientiMacchine_MacchineStandard_MacchinaStandardIdMacchinaStandard",
                        column: x => x.MacchinaStandardIdMacchinaStandard,
                        principalTable: "MacchineStandard",
                        principalColumn: "IdMacchinaStandard",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Disegni",
                columns: table => new
                {
                    IdDisegno = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdPiastra = table.Column<int>(type: "INTEGER", nullable: false),
                    CodiceDisegno = table.Column<string>(type: "TEXT", nullable: true),
                    NomeFile = table.Column<string>(type: "TEXT", nullable: true),
                    PercorsoFile = table.Column<string>(type: "TEXT", nullable: true),
                    VaultId = table.Column<string>(type: "TEXT", nullable: true),
                    Revisione = table.Column<string>(type: "TEXT", nullable: true),
                    Formato = table.Column<string>(type: "TEXT", nullable: true),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    DataUltimaModificaFile = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
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
                    IdCompatibilita = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdPiastra = table.Column<int>(type: "INTEGER", nullable: false),
                    IdMacchinaStandard = table.Column<int>(type: "INTEGER", nullable: false),
                    FonteDato = table.Column<int>(type: "INTEGER", nullable: true),
                    DataVerifica = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UtenteVerifica = table.Column<string>(type: "TEXT", nullable: true),
                    Attiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    PiastraIdPiastra = table.Column<int>(type: "INTEGER", nullable: false),
                    MacchinaStandardIdMacchinaStandard = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiastreMacchineCompatibili", x => x.IdCompatibilita);
                    table.ForeignKey(
                        name: "FK_PiastreMacchineCompatibili_MacchineStandard_MacchinaStandardIdMacchinaStandard",
                        column: x => x.MacchinaStandardIdMacchinaStandard,
                        principalTable: "MacchineStandard",
                        principalColumn: "IdMacchinaStandard",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PiastreMacchineCompatibili_Piastre_PiastraIdPiastra",
                        column: x => x.PiastraIdPiastra,
                        principalTable: "Piastre",
                        principalColumn: "IdPiastra",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientiPiastre",
                columns: table => new
                {
                    IdClientePiastra = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    IdPiastra = table.Column<int>(type: "INTEGER", nullable: false),
                    IdClienteMacchina = table.Column<int>(type: "INTEGER", nullable: true),
                    DataAssociazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    ClienteIdCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    PiastraIdPiastra = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiPiastre", x => x.IdClientePiastra);
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_ClientiMacchine_IdClienteMacchina",
                        column: x => x.IdClienteMacchina,
                        principalTable: "ClientiMacchine",
                        principalColumn: "IdClienteMacchina",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_Clienti_ClienteIdCliente",
                        column: x => x.ClienteIdCliente,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_Piastre_PiastraIdPiastra",
                        column: x => x.PiastraIdPiastra,
                        principalTable: "Piastre",
                        principalColumn: "IdPiastra",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clienti_CodiceClienteGestionale",
                table: "Clienti",
                column: "CodiceClienteGestionale",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientiMacchine_ClienteIdCliente",
                table: "ClientiMacchine",
                column: "ClienteIdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiMacchine_MacchinaStandardIdMacchinaStandard",
                table: "ClientiMacchine",
                column: "MacchinaStandardIdMacchinaStandard");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_ClienteIdCliente",
                table: "ClientiPiastre",
                column: "ClienteIdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdCliente_IdPiastra",
                table: "ClientiPiastre",
                columns: new[] { "IdCliente", "IdPiastra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdClienteMacchina",
                table: "ClientiPiastre",
                column: "IdClienteMacchina");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_PiastraIdPiastra",
                table: "ClientiPiastre",
                column: "PiastraIdPiastra");

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
                filter: "\"CodiceArticoloGestionale\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_CodicePiastra",
                table: "Piastre",
                column: "CodicePiastra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_IdPiastra_IdMacchinaStandard",
                table: "PiastreMacchineCompatibili",
                columns: new[] { "IdPiastra", "IdMacchinaStandard" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_MacchinaStandardIdMacchinaStandard",
                table: "PiastreMacchineCompatibili",
                column: "MacchinaStandardIdMacchinaStandard");

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_PiastraIdPiastra",
                table: "PiastreMacchineCompatibili",
                column: "PiastraIdPiastra");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientiPiastre");

            migrationBuilder.DropTable(
                name: "Disegni");

            migrationBuilder.DropTable(
                name: "PiastreMacchineCompatibili");

            migrationBuilder.DropTable(
                name: "ClientiMacchine");

            migrationBuilder.DropTable(
                name: "Piastre");

            migrationBuilder.DropTable(
                name: "Clienti");

            migrationBuilder.DropTable(
                name: "MacchineStandard");
        }
    }
}
