using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeAutomation.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLogCategoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Logs_Category",
                table: "Logs",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Logs_Category",
                table: "Logs");
        }
    }
}
