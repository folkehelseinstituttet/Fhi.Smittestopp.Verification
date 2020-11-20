using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fhi.Smittestopp.Verification.Persistence.Migrations
{
    public partial class VerificationRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pseudonym = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedAtTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationRecords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationRecords");
        }
    }
}
