using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProduitFournisseurLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Produits_Fournisseurs_FournisseurId",
                table: "Produits");

            migrationBuilder.DropIndex(
                name: "IX_Produits_FournisseurId",
                table: "Produits");

            migrationBuilder.DropColumn(
                name: "FournisseurId",
                table: "Produits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FournisseurId",
                table: "Produits",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Produits_FournisseurId",
                table: "Produits",
                column: "FournisseurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Produits_Fournisseurs_FournisseurId",
                table: "Produits",
                column: "FournisseurId",
                principalTable: "Fournisseurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
