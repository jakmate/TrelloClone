using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrelloClone.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskItemPosition2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "Tasks");
        }
    }
}
