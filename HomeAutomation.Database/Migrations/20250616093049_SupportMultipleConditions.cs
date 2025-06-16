using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeAutomation.Database.Migrations
{
    /// <inheritdoc />
    public partial class SupportMultipleConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Conditions_ConditionId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Triggers_Conditions_ConditionId",
                table: "Triggers");

            migrationBuilder.DropIndex(
                name: "IX_Triggers_ConditionId",
                table: "Triggers");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ConditionId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ConditionId",
                table: "Triggers");

            migrationBuilder.DropColumn(
                name: "ConditionId",
                table: "Actions");

            migrationBuilder.CreateTable(
                name: "ActionEntityConditionEntity",
                columns: table => new
                {
                    ActionsId = table.Column<int>(type: "integer", nullable: false),
                    ConditionsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionEntityConditionEntity", x => new { x.ActionsId, x.ConditionsId });
                    table.ForeignKey(
                        name: "FK_ActionEntityConditionEntity_Actions_ActionsId",
                        column: x => x.ActionsId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionEntityConditionEntity_Conditions_ConditionsId",
                        column: x => x.ConditionsId,
                        principalTable: "Conditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConditionEntityTriggerEntity",
                columns: table => new
                {
                    ConditionsId = table.Column<int>(type: "integer", nullable: false),
                    TriggersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionEntityTriggerEntity", x => new { x.ConditionsId, x.TriggersId });
                    table.ForeignKey(
                        name: "FK_ConditionEntityTriggerEntity_Conditions_ConditionsId",
                        column: x => x.ConditionsId,
                        principalTable: "Conditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConditionEntityTriggerEntity_Triggers_TriggersId",
                        column: x => x.TriggersId,
                        principalTable: "Triggers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionEntityConditionEntity_ConditionsId",
                table: "ActionEntityConditionEntity",
                column: "ConditionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionEntityTriggerEntity_TriggersId",
                table: "ConditionEntityTriggerEntity",
                column: "TriggersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionEntityConditionEntity");

            migrationBuilder.DropTable(
                name: "ConditionEntityTriggerEntity");

            migrationBuilder.AddColumn<int>(
                name: "ConditionId",
                table: "Triggers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConditionId",
                table: "Actions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Triggers_ConditionId",
                table: "Triggers",
                column: "ConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ConditionId",
                table: "Actions",
                column: "ConditionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Conditions_ConditionId",
                table: "Actions",
                column: "ConditionId",
                principalTable: "Conditions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Triggers_Conditions_ConditionId",
                table: "Triggers",
                column: "ConditionId",
                principalTable: "Conditions",
                principalColumn: "Id");
        }
    }
}
