using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

#nullable disable

namespace HomeAutomation.Database.Migrations.DefaultMigrations;

/// <inheritdoc />
public partial class mergeWithLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PhoneCalls");

        migrationBuilder.CreateTable(
            name: "MailMessages",
            columns: table => new
            {
                ID = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MessageID = table.Column<string>(type: "text", nullable: false),
                DeviceSource = table.Column<string>(type: "text", nullable: true),
                DeviceSourceID = table.Column<string>(type: "text", nullable: true),
                EmlData = table.Column<byte[]>(type: "bytea", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MailMessages", x => x.ID);
            });

        migrationBuilder.CreateTable(
            name: "Rows",
            columns: table => new
            {
                ID = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Level = table.Column<int>(type: "integer", nullable: false),
                Category = table.Column<string>(type: "text", nullable: false),
                EventID = table.Column<int>(type: "integer", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Message = table.Column<string>(type: "text", nullable: false),
                Exception = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Rows", x => x.ID);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MailMessages");

        migrationBuilder.DropTable(
            name: "Rows");

        migrationBuilder.CreateTable(
            name: "PhoneCalls",
            columns: table => new
            {
                ID = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Length = table.Column<TimeSpan>(type: "interval", nullable: false),
                Number = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PhoneCalls", x => x.ID);
            });
    }
}
