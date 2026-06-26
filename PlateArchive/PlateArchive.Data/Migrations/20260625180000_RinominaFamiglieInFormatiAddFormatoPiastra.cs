using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class RinominaFamiglieInFormatiAddFormatoPiastra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Rimuovi FK e indici che dipendono dai vecchi nomi ─────────
            migrationBuilder.DropForeignKey(
                name: "FK_MacchineStandard_FamiglieMacchine_IdFamiglia",
                table: "MacchineStandard");

            migrationBuilder.DropIndex(
                name: "IX_FamiglieMacchine_NomeFamiglia",
                table: "FamiglieMacchine");

            migrationBuilder.DropIndex(
                name: "IX_MacchineStandard_IdFamiglia",
                table: "MacchineStandard");

            // ── 2. Rinomina tabella e colonne ────────────────────────────────
            migrationBuilder.RenameTable(
                name: "FamiglieMacchine",
                newName: "FormatiMacchine");

            migrationBuilder.RenameColumn(
                name: "IdFamiglia",
                table: "FormatiMacchine",
                newName: "IdFormato");

            migrationBuilder.RenameColumn(
                name: "NomeFamiglia",
                table: "FormatiMacchine",
                newName: "NomeFormato");

            migrationBuilder.RenameColumn(
                name: "IdFamiglia",
                table: "MacchineStandard",
                newName: "IdFormato");

            // ── 3. Ricrea indici con nuovi nomi ──────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_FormatiMacchine_NomeFormato",
                table: "FormatiMacchine",
                column: "NomeFormato",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MacchineStandard_IdFormato",
                table: "MacchineStandard",
                column: "IdFormato");

            // ── 4. Ricrea FK MacchineStandard → FormatiMacchine ───────────────
            migrationBuilder.AddForeignKey(
                name: "FK_MacchineStandard_FormatiMacchine_IdFormato",
                table: "MacchineStandard",
                column: "IdFormato",
                principalTable: "FormatiMacchine",
                principalColumn: "IdFormato",
                onDelete: ReferentialAction.NoAction);

            // ── 5. Aggiungi IdFormato a Piastre ──────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "IdFormato",
                table: "Piastre",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_IdFormato",
                table: "Piastre",
                column: "IdFormato");

            migrationBuilder.AddForeignKey(
                name: "FK_Piastre_FormatiMacchine_IdFormato",
                table: "Piastre",
                column: "IdFormato",
                principalTable: "FormatiMacchine",
                principalColumn: "IdFormato",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Piastre_FormatiMacchine_IdFormato",
                table: "Piastre");

            migrationBuilder.DropForeignKey(
                name: "FK_MacchineStandard_FormatiMacchine_IdFormato",
                table: "MacchineStandard");

            migrationBuilder.DropIndex(
                name: "IX_Piastre_IdFormato",
                table: "Piastre");

            migrationBuilder.DropColumn(
                name: "IdFormato",
                table: "Piastre");

            migrationBuilder.DropIndex(
                name: "IX_MacchineStandard_IdFormato",
                table: "MacchineStandard");

            migrationBuilder.DropIndex(
                name: "IX_FormatiMacchine_NomeFormato",
                table: "FormatiMacchine");

            migrationBuilder.RenameColumn(
                name: "IdFormato",
                table: "MacchineStandard",
                newName: "IdFamiglia");

            migrationBuilder.RenameColumn(
                name: "NomeFormato",
                table: "FormatiMacchine",
                newName: "NomeFamiglia");

            migrationBuilder.RenameColumn(
                name: "IdFormato",
                table: "FormatiMacchine",
                newName: "IdFamiglia");

            migrationBuilder.RenameTable(
                name: "FormatiMacchine",
                newName: "FamiglieMacchine");

            migrationBuilder.CreateIndex(
                name: "IX_FamiglieMacchine_NomeFamiglia",
                table: "FamiglieMacchine",
                column: "NomeFamiglia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MacchineStandard_IdFamiglia",
                table: "MacchineStandard",
                column: "IdFamiglia");

            migrationBuilder.AddForeignKey(
                name: "FK_MacchineStandard_FamiglieMacchine_IdFamiglia",
                table: "MacchineStandard",
                column: "IdFamiglia",
                principalTable: "FamiglieMacchine",
                principalColumn: "IdFamiglia",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
