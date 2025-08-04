using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrelloClone.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "Tasks");

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Boards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "Boards");

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
