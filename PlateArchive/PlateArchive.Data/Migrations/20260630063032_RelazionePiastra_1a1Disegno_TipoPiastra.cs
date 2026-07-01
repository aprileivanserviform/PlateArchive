using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateArchive.Data.Migrations
{
    /// <inheritdoc />
    public partial class RelazionePiastra_1a1Disegno_TipoPiastra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Disegni_Piastre_IdPiastra",
                table: "Disegni");

            migrationBuilder.DropForeignKey(
                name: "FK_MacchineStandard_ProduttoriMacchine_IdProduttore",
                table: "MacchineStandard");

            migrationBuilder.DropIndex(
                name: "IX_ProduttoriMacchine_NomeProduttore",
                table: "ProduttoriMacchine");

            migrationBuilder.DropIndex(
                name: "IX_PiastreMacchineCompatibili_IdPiastra",
                table: "PiastreMacchineCompatibili");

            migrationBuilder.DropIndex(
                name: "IX_FormatiMacchine_NomeFormato",
                table: "FormatiMacchine");

            migrationBuilder.DropIndex(
                name: "IX_Disegni_IdPiastra",
                table: "Disegni");

            migrationBuilder.DropIndex(
                name: "IX_ClientiPiastre_IdCliente",
                table: "ClientiPiastre");

            migrationBuilder.AlterColumn<string>(
                name: "NomeProduttore",
                table: "ProduttoriMacchine",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Peso",
                table: "Piastre",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdClienteEsclusivo",
                table: "Piastre",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoPiastra",
                table: "Piastre",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "NomeFormato",
                table: "FormatiMacchine",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "IdPiastra",
                table: "Disegni",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Piastre_IdClienteEsclusivo",
                table: "Piastre",
                column: "IdClienteEsclusivo");

            migrationBuilder.CreateIndex(
                name: "IX_Disegni_IdPiastra",
                table: "Disegni",
                column: "IdPiastra",
                unique: true,
                filter: "[IdPiastra] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Disegni_Piastre_IdPiastra",
                table: "Disegni",
                column: "IdPiastra",
                principalTable: "Piastre",
                principalColumn: "IdPiastra",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MacchineStandard_ProduttoriMacchine_IdProduttore",
                table: "MacchineStandard",
                column: "IdProduttore",
                principalTable: "ProduttoriMacchine",
                principalColumn: "IdProduttore",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Piastre_Clienti_IdClienteEsclusivo",
                table: "Piastre",
                column: "IdClienteEsclusivo",
                principalTable: "Clienti",
                principalColumn: "IdCliente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Disegni_Piastre_IdPiastra",
                table: "Disegni");

            migrationBuilder.DropForeignKey(
                name: "FK_MacchineStandard_ProduttoriMacchine_IdProduttore",
                table: "MacchineStandard");

            migrationBuilder.DropForeignKey(
                name: "FK_Piastre_Clienti_IdClienteEsclusivo",
                table: "Piastre");

            migrationBuilder.DropIndex(
                name: "IX_Piastre_IdClienteEsclusivo",
                table: "Piastre");

            migrationBuilder.DropIndex(
                name: "IX_Disegni_IdPiastra",
                table: "Disegni");

            migrationBuilder.DropColumn(
                name: "IdClienteEsclusivo",
                table: "Piastre");

            migrationBuilder.DropColumn(
                name: "TipoPiastra",
                table: "Piastre");

            migrationBuilder.AlterColumn<string>(
                name: "NomeProduttore",
                table: "ProduttoriMacchine",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Peso",
                table: "Piastre",
                type: "decimal(18,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NomeFormato",
                table: "FormatiMacchine",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "IdPiastra",
                table: "Disegni",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProduttoriMacchine_NomeProduttore",
                table: "ProduttoriMacchine",
                column: "NomeProduttore",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PiastreMacchineCompatibili_IdPiastra",
                table: "PiastreMacchineCompatibili",
                column: "IdPiastra");

            migrationBuilder.CreateIndex(
                name: "IX_FormatiMacchine_NomeFormato",
                table: "FormatiMacchine",
                column: "NomeFormato",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disegni_IdPiastra",
                table: "Disegni",
                column: "IdPiastra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientiPiastre_IdCliente",
                table: "ClientiPiastre",
                column: "IdCliente");

            migrationBuilder.AddForeignKey(
                name: "FK_Disegni_Piastre_IdPiastra",
                table: "Disegni",
                column: "IdPiastra",
                principalTable: "Piastre",
                principalColumn: "IdPiastra",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MacchineStandard_ProduttoriMacchine_IdProduttore",
                table: "MacchineStandard",
                column: "IdProduttore",
                principalTable: "ProduttoriMacchine",
                principalColumn: "IdProduttore");
        }
    }
}
