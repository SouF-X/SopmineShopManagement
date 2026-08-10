using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PaymentRevision",
                table: "Invoices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "InvoicePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsOpeningBalance = table.Column<bool>(type: "bit", nullable: false),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePayments", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_InvoicePayments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayments_InvoiceId_PaymentDate",
                table: "InvoicePayments",
                columns: new[] { "InvoiceId", "PaymentDate" });

            migrationBuilder.Sql(@"
INSERT INTO [InvoicePayments] ([Id], [InvoiceId], [Amount], [PaymentDate], [Method], [Reference], [Note], [IsOpeningBalance], [CancelledAtUtc], [CancellationReason], [CreatedAtUtc], [CreatedBy], [LastModifiedUtc], [LastModifiedBy])
SELECT [invoice].[Id], [invoice].[Id], [invoice].[Total], [invoice].[Date], [invoice].[PaymentMethod], CONCAT(N'MIGRATION-', CONVERT(nvarchar(36), [invoice].[Id])), NULL, CAST(1 AS bit), NULL, NULL, [invoice].[CreatedAtUtc], NULL, [invoice].[LastModifiedUtc], NULL
FROM [Invoices] AS [invoice]
WHERE ([invoice].[PaymentStatus] = 1 OR [invoice].[Status] = 2)
  AND [invoice].[Total] > 0
  AND NOT EXISTS (
      SELECT 1
      FROM [InvoicePayments] AS [payment]
      WHERE [payment].[InvoiceId] = [invoice].[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversing this migration removes the synthetic opening-balance payment history.
            migrationBuilder.DropTable(
                name: "InvoicePayments");

            migrationBuilder.DropColumn(
                name: "PaymentRevision",
                table: "Invoices");
        }
    }
}
