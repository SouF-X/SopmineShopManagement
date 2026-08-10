using System;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using SopmineWorkshop.Infrastructure.Data;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260707090000_AddProduitFournisseurLink")]
    public partial class AddProduitFournisseurLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FournisseurId",
                table: "Produits",
                type: "uniqueidentifier",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
