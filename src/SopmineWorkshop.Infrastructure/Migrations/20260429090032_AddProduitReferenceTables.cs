using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProduitReferenceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FamillesProduit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamillesProduit", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "UnitesMesure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitesMesure", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.Sql("""
                INSERT INTO FamillesProduit (Id, Libelle, CreatedAtUtc, LastModifiedUtc)
                SELECT NEWID(), MIN(LTRIM(RTRIM(Famille))), SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                FROM Produits
                WHERE Famille IS NOT NULL AND LTRIM(RTRIM(Famille)) <> ''
                GROUP BY LOWER(LTRIM(RTRIM(Famille)));
                """);

            migrationBuilder.Sql("""
                INSERT INTO UnitesMesure (Id, Libelle, CreatedAtUtc, LastModifiedUtc)
                SELECT NEWID(), MIN(LTRIM(RTRIM(Unite))), SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                FROM Produits
                WHERE Unite IS NOT NULL AND LTRIM(RTRIM(Unite)) <> ''
                GROUP BY LOWER(LTRIM(RTRIM(Unite)));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FamillesProduit_Libelle",
                table: "FamillesProduit",
                column: "Libelle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitesMesure_Libelle",
                table: "UnitesMesure",
                column: "Libelle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamillesProduit");

            migrationBuilder.DropTable(
                name: "UnitesMesure");
        }
    }
}
