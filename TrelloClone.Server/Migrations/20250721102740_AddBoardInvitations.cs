using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrelloClone.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PermissionLevel",
                table: "BoardUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BoardInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BoardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvitedUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InviterUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardInvitations_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardInvitations_Users_InvitedUserId",
                        column: x => x.InvitedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BoardInvitations_Users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardInvitations_BoardId",
                table: "BoardInvitations",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardInvitations_InvitedUserId",
                table: "BoardInvitations",
                column: "InvitedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardInvitations_InviterUserId",
                table: "BoardInvitations",
                column: "InviterUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardInvitations");

            migrationBuilder.DropColumn(
                name: "PermissionLevel",
                table: "BoardUsers");
        }
    }
}
