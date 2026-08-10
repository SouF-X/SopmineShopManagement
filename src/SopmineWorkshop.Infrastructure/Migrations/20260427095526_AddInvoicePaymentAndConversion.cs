using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentAndConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConvertedToInvoiceId",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Invoices
                SET PaymentStatus = 0
                WHERE PaymentStatus IS NULL
                  AND (
                    (Nature = 0 AND Type IN (2, 4))
                    OR (Nature = 1 AND Type IN (3, 4))
                  )
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertedToInvoiceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Invoices");
        }
    }
}
