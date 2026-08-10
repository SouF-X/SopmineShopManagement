using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenInvoiceBusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Invoices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "Invoices",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxTotal",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineSubtotal",
                table: "InvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTax",
                table: "InvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "InvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductFamily",
                table: "InvoiceLines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "InvoiceLines",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductReference",
                table: "InvoiceLines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductUnit",
                table: "InvoiceLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE invoiceLine
                SET
                    ProductReference = produit.Reference,
                    ProductName = produit.Nom,
                    ProductFamily = produit.Famille,
                    ProductUnit = produit.Unite,
                    LineSubtotal = ROUND(invoiceLine.Quantity * invoiceLine.Price, 2),
                    LineTax = ROUND(ROUND(invoiceLine.Quantity * invoiceLine.Price, 2) * (invoiceLine.TVA / 100), 2),
                    LineTotal = ROUND(
                        ROUND(invoiceLine.Quantity * invoiceLine.Price, 2)
                        + ROUND(ROUND(invoiceLine.Quantity * invoiceLine.Price, 2) * (invoiceLine.TVA / 100), 2),
                        2)
                FROM InvoiceLines AS invoiceLine
                INNER JOIN Produits AS produit ON produit.Id = invoiceLine.ProduitId;
                """);

            migrationBuilder.Sql(
                """
                UPDATE invoice
                SET
                    Subtotal = totals.Subtotal,
                    TaxTotal = totals.TaxTotal,
                    Total = totals.Total
                FROM Invoices AS invoice
                INNER JOIN (
                    SELECT
                        InvoiceId,
                        ROUND(SUM(LineSubtotal), 2) AS Subtotal,
                        ROUND(SUM(LineTax), 2) AS TaxTotal,
                        ROUND(SUM(LineTotal), 2) AS Total
                    FROM InvoiceLines
                    GROUP BY InvoiceId
                ) AS totals ON totals.InvoiceId = invoice.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxTotal",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LineSubtotal",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "LineTax",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "ProductFamily",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "ProductReference",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "ProductUnit",
                table: "InvoiceLines");
        }
    }
}
