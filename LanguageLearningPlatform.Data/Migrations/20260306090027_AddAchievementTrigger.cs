using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageLearningPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TriggerType",
                table: "Achievements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TriggerValue",
                table: "Achievements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "TriggerValue",
                table: "Achievements");
        }
    }
}
