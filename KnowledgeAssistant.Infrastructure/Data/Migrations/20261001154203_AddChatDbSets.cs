using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeAssistant.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatDbSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_ChatSession_ChatSessionId",
                table: "ChatMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatSession_Users_UserId",
                table: "ChatSession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatSession",
                table: "ChatSession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage");

            migrationBuilder.RenameTable(
                name: "ChatSession",
                newName: "ChatSessions");

            migrationBuilder.RenameTable(
                name: "ChatMessage",
                newName: "ChatMessages");

            migrationBuilder.RenameIndex(
                name: "IX_ChatSession_UserId_UpdatedAt",
                table: "ChatSessions",
                newName: "IX_ChatSessions_UserId_UpdatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessage_ChatSessionId_CreatedAt",
                table: "ChatMessages",
                newName: "IX_ChatMessages_ChatSessionId_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatSessions",
                table: "ChatSessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                table: "ChatMessages",
                column: "ChatSessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Users_UserId",
                table: "ChatSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_Users_UserId",
                table: "ChatSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatSessions",
                table: "ChatSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessages");

            migrationBuilder.RenameTable(
                name: "ChatSessions",
                newName: "ChatSession");

            migrationBuilder.RenameTable(
                name: "ChatMessages",
                newName: "ChatMessage");

            migrationBuilder.RenameIndex(
                name: "IX_ChatSessions_UserId_UpdatedAt",
                table: "ChatSession",
                newName: "IX_ChatSession_UserId_UpdatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessages_ChatSessionId_CreatedAt",
                table: "ChatMessage",
                newName: "IX_ChatMessage_ChatSessionId_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatSession",
                table: "ChatSession",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_ChatSession_ChatSessionId",
                table: "ChatMessage",
                column: "ChatSessionId",
                principalTable: "ChatSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSession_Users_UserId",
                table: "ChatSession",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
