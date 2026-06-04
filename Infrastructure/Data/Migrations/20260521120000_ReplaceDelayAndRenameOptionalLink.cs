using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDelayAndRenameOptionalLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OptionalLink",
                table: "UserTasks",
                newName: "MaybeTargetUrl");

            migrationBuilder.AddColumn<int>(
                name: "Delay",
                table: "UserTasks",
                type: "integer",
                nullable: false,
                defaultValue: 2); // Normal

            migrationBuilder.DropColumn(
                name: "DelayFromSeconds",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "DelayToSeconds",
                table: "UserTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaybeTargetUrl",
                table: "UserTasks",
                newName: "OptionalLink");

            migrationBuilder.AddColumn<int>(
                name: "DelayFromSeconds",
                table: "UserTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DelayToSeconds",
                table: "UserTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropColumn(
                name: "Delay",
                table: "UserTasks");
        }
    }
}
