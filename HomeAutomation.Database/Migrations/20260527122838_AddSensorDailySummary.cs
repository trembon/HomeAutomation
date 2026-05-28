using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeAutomation.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorDailySummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SensorDailySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MinValue = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxValue = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgValue = table.Column<decimal>(type: "numeric", nullable: false),
                    FirstValue = table.Column<decimal>(type: "numeric", nullable: false),
                    LastValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ReadingCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorDailySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorDailySummaries_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SensorValues_DeviceId_Type_Timestamp",
                table: "SensorValues",
                columns: new[] { "DeviceId", "Type", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorDailySummaries_Date",
                table: "SensorDailySummaries",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SensorDailySummaries_DeviceId_Type_Date",
                table: "SensorDailySummaries",
                columns: new[] { "DeviceId", "Type", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorDailySummaries");

            migrationBuilder.DropIndex(
                name: "IX_SensorValues_DeviceId_Type_Timestamp",
                table: "SensorValues");
        }
    }
}
