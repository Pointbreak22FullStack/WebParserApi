using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionalLinkAndDelayRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Delay",
                table: "UserTasks",
                newName: "DelayToSeconds");

            migrationBuilder.AddColumn<int>(
                name: "DelayFromSeconds",
                table: "UserTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OptionalLink",
                table: "UserTasks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelayFromSeconds",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "OptionalLink",
                table: "UserTasks");

            migrationBuilder.RenameColumn(
                name: "DelayToSeconds",
                table: "UserTasks",
                newName: "Delay");
        }
    }
}
