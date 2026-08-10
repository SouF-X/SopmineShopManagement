using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SopmineWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentNominations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentNominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nature = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Root = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IncrementSize = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentNominations", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentNominations_Nature_Type",
                table: "DocumentNominations",
                columns: new[] { "Nature", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentNominations");
        }
    }
}
