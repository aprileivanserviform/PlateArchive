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
                    IdCategoriaPiastra = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codice = table.Column<string>(type: "TEXT", nullable: false),
                    Descrizione = table.Column<string>(type: "TEXT", nullable: false),
                    Ordine = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriePiastre", x => x.IdCategoriaPiastra);
                });

            migrationBuilder.CreateTable(
                name: "Clienti",
                columns: table => new
                {
                    IdCliente = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodiceClienteGestionale = table.Column<string>(type: "TEXT", nullable: false),
                    RagioneSociale = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    AttivoGestionale = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clienti", x => x.IdCliente);
                });

            migrationBuilder.CreateTable(
                name: "FormatiMacchine",
                columns: table => new
                {
                    IdFormato = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeFormato = table.Column<string>(type: "TEXT", nullable: false),
                    IsEliminata = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormatiMacchine", x => x.IdFormato);
                });

            migrationBuilder.CreateTable(
                name: "ProduttoriMacchine",
                columns: table => new
                {
                    IdProduttore = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeProduttore = table.Column<string>(type: "TEXT", nullable: false),
                    IsEliminata = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProduttoriMacchine", x => x.IdProduttore);
                });

            migrationBuilder.CreateTable(
                name: "AllegatiClienti",
                columns: table => new
                {
                    IdAllegato = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    NomeFile = table.Column<string>(type: "TEXT", nullable: false),
                    PercorsoFile = table.Column<string>(type: "TEXT", nullable: false),
                    DimensioneBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    DataCaricamento = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    IdNota = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    Titolo = table.Column<string>(type: "TEXT", nullable: false),
                    Testo = table.Column<string>(type: "TEXT", nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataModifica = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Piastre",
                columns: table => new
                {
                    IdPiastra = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodicePiastra = table.Column<string>(type: "TEXT", nullable: false),
                    Descrizione = table.Column<string>(type: "TEXT", nullable: true),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoPiastra = table.Column<int>(type: "INTEGER", nullable: false),
                    IdClienteEsclusivo = table.Column<int>(type: "INTEGER", nullable: true),
                    IdCategoriaPiastra = table.Column<int>(type: "INTEGER", nullable: true),
                    IdFormato = table.Column<int>(type: "INTEGER", nullable: true),
                    IsEliminata = table.Column<bool>(type: "INTEGER", nullable: false),
                    LarghezzaMm = table.Column<decimal>(type: "TEXT", nullable: true),
                    AltezzaMm = table.Column<decimal>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataUltimaModifica = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_Piastre_Clienti_IdClienteEsclusivo",
                        column: x => x.IdClienteEsclusivo,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente");
                    table.ForeignKey(
                        name: "FK_Piastre_FormatiMacchine_IdFormato",
                        column: x => x.IdFormato,
                        principalTable: "FormatiMacchine",
                        principalColumn: "IdFormato");
                });

            migrationBuilder.CreateTable(
                name: "MacchineStandard",
                columns: table => new
                {
                    IdMacchinaStandard = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodiceMacchina = table.Column<string>(type: "TEXT", nullable: false),
                    NomeMacchina = table.Column<string>(type: "TEXT", nullable: false),
                    IdFormato = table.Column<int>(type: "INTEGER", nullable: true),
                    IdProduttore = table.Column<int>(type: "INTEGER", nullable: true),
                    Versione = table.Column<string>(type: "TEXT", nullable: true),
                    Attiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacchineStandard", x => x.IdMacchinaStandard);
                    table.ForeignKey(
                        name: "FK_MacchineStandard_FormatiMacchine_IdFormato",
                        column: x => x.IdFormato,
                        principalTable: "FormatiMacchine",
                        principalColumn: "IdFormato");
                    table.ForeignKey(
                        name: "FK_MacchineStandard_ProduttoriMacchine_IdProduttore",
                        column: x => x.IdProduttore,
                        principalTable: "ProduttoriMacchine",
                        principalColumn: "IdProduttore",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Disegni",
                columns: table => new
                {
                    IdDisegno = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdPiastra = table.Column<int>(type: "INTEGER", nullable: true),
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
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ClientiMacchine",
                columns: table => new
                {
                    IdClienteMacchina = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    IdMacchinaStandard = table.Column<int>(type: "INTEGER", nullable: false),
                    CodiceInternoCliente = table.Column<string>(type: "TEXT", nullable: true),
                    DataAssociazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Attiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
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
                    Note = table.Column<string>(type: "TEXT", nullable: true)
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
                    IdClientePiastra = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdCliente = table.Column<int>(type: "INTEGER", nullable: false),
                    IdPiastra = table.Column<int>(type: "INTEGER", nullable: false),
                    IdClienteMacchina = table.Column<int>(type: "INTEGER", nullable: true),
                    DataAssociazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientiPiastre", x => x.IdClientePiastra);
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_ClientiMacchine_IdClienteMacchina",
                        column: x => x.IdClienteMacchina,
                        principalTable: "ClientiMacchine",
                        principalColumn: "IdClienteMacchina");
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_Clienti_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientiPiastre_Piastre_IdPiastra",
                        column: x => x.IdPiastra,
                        principalTable: "Piastre",
                        principalColumn: "IdPiastra",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiClienti_IdCliente",
                table: "AllegatiClienti",
                column: "IdCliente");

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
                name: "IX_ClientiPiastre_IdCliente_IdPiastra",
                table: "ClientiPiastre",
                columns: new[] { "IdCliente", "IdPiastra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdClienteMacchina",
                table: "ClientiPiastre",
                column: "IdClienteMacchina");

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdPiastra",
                table: "ClientiPiastre",
                column: "IdPiastra");

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
                name: "IX_MacchineStandard_IdFormato",
                table: "MacchineStandard",
                column: "IdFormato");

            migrationBuilder.CreateIndex(
                name: "IX_MacchineStandard_IdProduttore",
                table: "MacchineStandard",
                column: "IdProduttore");

            migrationBuilder.CreateIndex(
                name: "IX_NoteTecnicheClienti_IdCliente",
                table: "NoteTecnicheClienti",
                column: "IdCliente");

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
                name: "IX_Piastre_IdClienteEsclusivo",
                table: "Piastre",
                column: "IdClienteEsclusivo");

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_IdFormato",
                table: "Piastre",
                column: "IdFormato");

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_IdMacchinaStandard",
                table: "PiastreMacchineCompatibili",
                column: "IdMacchinaStandard");

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_IdPiastra_IdMacchinaStandard",
                table: "PiastreMacchineCompatibili",
                columns: new[] { "IdPiastra", "IdMacchinaStandard" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllegatiClienti");

            migrationBuilder.DropTable(
                name: "ClientiPiastre");

            migrationBuilder.DropTable(
                name: "Disegni");

            migrationBuilder.DropTable(
                name: "NoteTecnicheClienti");

            migrationBuilder.DropTable(
                name: "PiastreMacchineCompatibili");

            migrationBuilder.DropTable(
                name: "ClientiMacchine");

            migrationBuilder.DropTable(
                name: "Piastre");

            migrationBuilder.DropTable(
                name: "MacchineStandard");

            migrationBuilder.DropTable(
                name: "CategoriePiastre");

            migrationBuilder.DropTable(
                name: "Clienti");

            migrationBuilder.DropTable(
                name: "FormatiMacchine");

            migrationBuilder.DropTable(
                name: "ProduttoriMacchine");
        }
    }
}
