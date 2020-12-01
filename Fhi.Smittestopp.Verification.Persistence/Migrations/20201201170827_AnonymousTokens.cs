using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fhi.Smittestopp.Verification.Persistence.Migrations
{
    public partial class AnonymousTokens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnonymousTokenIssueRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JwtTokenId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JwtTokenExpiry = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymousTokenIssueRecords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnonymousTokenIssueRecords");
        }
    }
}
