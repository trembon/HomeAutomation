using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeAutomation.Database.Migrations.DefaultMigrations
{
    /// <inheritdoc />
    public partial class addsourcetosensorvalues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TellstickID",
                table: "SensorValues",
                newName: "Source");

            migrationBuilder.AddColumn<string>(
                name: "SourceID",
                table: "SensorValues",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceID",
                table: "SensorValues");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "SensorValues",
                newName: "TellstickID");
        }
    }
}
