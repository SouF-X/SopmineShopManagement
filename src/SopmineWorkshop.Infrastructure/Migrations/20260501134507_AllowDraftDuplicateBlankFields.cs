using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowDraftDuplicateBlankFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitesMesure_Libelle",
                table: "UnitesMesure");

            migrationBuilder.DropIndex(
                name: "IX_Produits_Reference",
                table: "Produits");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Reference",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_FamillesProduit_Libelle",
                table: "FamillesProduit");

            migrationBuilder.CreateIndex(
                name: "IX_UnitesMesure_Libelle",
                table: "UnitesMesure",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_Produits_Reference",
                table: "Produits",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Reference",
                table: "Invoices",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_FamillesProduit_Libelle",
                table: "FamillesProduit",
                column: "Libelle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitesMesure_Libelle",
                table: "UnitesMesure");

            migrationBuilder.DropIndex(
                name: "IX_Produits_Reference",
                table: "Produits");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Reference",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_FamillesProduit_Libelle",
                table: "FamillesProduit");

            migrationBuilder.CreateIndex(
                name: "IX_UnitesMesure_Libelle",
                table: "UnitesMesure",
                column: "Libelle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produits_Reference",
                table: "Produits",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Reference",
                table: "Invoices",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamillesProduit_Libelle",
                table: "FamillesProduit",
                column: "Libelle",
                unique: true);
        }
    }
}
