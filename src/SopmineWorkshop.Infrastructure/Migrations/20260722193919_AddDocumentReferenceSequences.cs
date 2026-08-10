using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    public partial class AddDocumentReferenceSequences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM [Invoices]
    WHERE NULLIF(LTRIM(RTRIM([Reference])) COLLATE Latin1_General_100_BIN2, N'') IS NOT NULL
    GROUP BY LTRIM(RTRIM([Reference])) COLLATE Latin1_General_100_BIN2
    HAVING COUNT(*) > 1)
    THROW 51000, 'Cannot create the unique invoice reference index: duplicate trimmed nonblank invoice references exist. Resolve the duplicate references before applying this migration.', 1;");

            migrationBuilder.DropIndex(name: "IX_Invoices_Reference", table: "Invoices");

            migrationBuilder.CreateTable(
                name: "DocumentReferenceSequences",
                columns: table => new
                {
                    Scope = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, collation: "Latin1_General_100_BIN2"),
                    LastSequence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DocumentReferenceSequences", x => x.Scope));

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Reference",
                table: "Invoices",
                column: "Reference",
                unique: true,
                filter: "[Reference] <> N''");

            migrationBuilder.Sql(@"
DECLARE @year nvarchar(2) = RIGHT(CONVERT(nvarchar(4), DATEPART(YEAR, SYSUTCDATETIME())), 2);
;WITH [Definitions] AS (
    SELECT 0 AS [Nature], 1 AS [Type], N'BC' AS [Code] UNION ALL
    SELECT 0, 2, N'BR-A' UNION ALL
    SELECT 0, 4, N'FA-A' UNION ALL
    SELECT 0, 5, N'AV-A' UNION ALL
    SELECT 1, 0, N'DV' UNION ALL
    SELECT 1, 1, N'BC-V' UNION ALL
    SELECT 1, 3, N'BL' UNION ALL
    SELECT 1, 4, N'FA' UNION ALL
    SELECT 1, 5, N'AV'
)
INSERT INTO [DocumentNominations] ([Id], [Nature], [Type], [Root], [DateFormat], [IncrementSize], [CreatedAtUtc], [LastModifiedUtc])
SELECT NEWID(), d.[Nature], d.[Type], @year + d.[Code], N'MM', 3, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM [Definitions] d
WHERE NOT EXISTS (
    SELECT 1 FROM [DocumentNominations] n
    WHERE n.[Nature] = d.[Nature] AND n.[Type] = d.[Type]);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DocumentReferenceSequences");
            migrationBuilder.DropIndex(name: "IX_Invoices_Reference", table: "Invoices");
            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_BIN2");
            migrationBuilder.CreateIndex(name: "IX_Invoices_Reference", table: "Invoices", column: "Reference");
        }
    }
}
