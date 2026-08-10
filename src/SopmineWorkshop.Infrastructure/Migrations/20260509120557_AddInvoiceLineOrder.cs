using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLineOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LineOrder",
                table: "InvoiceLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                WITH OrderedLines AS (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY InvoiceId
                            ORDER BY CreatedAtUtc, Id
                        ) AS LineOrder
                    FROM InvoiceLines
                )
                UPDATE invoiceLine
                SET LineOrder = orderedLine.LineOrder
                FROM InvoiceLines AS invoiceLine
                INNER JOIN OrderedLines AS orderedLine ON orderedLine.Id = invoiceLine.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineOrder",
                table: "InvoiceLines");
        }
    }
}
