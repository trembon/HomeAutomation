using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeAutomation.Migrations.Log
{
    public partial class _20211227_IncludeMailMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailMessages",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageID = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceSource = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceSourceID = table.Column<string>(type: "TEXT", nullable: true),
                    EmlData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailMessages", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailMessages");
        }
    }
}
