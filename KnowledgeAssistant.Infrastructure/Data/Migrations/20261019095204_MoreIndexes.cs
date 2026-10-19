using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeAssistant.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoreIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationRecipients_UserId_IsRead_NotificationId",
                table: "NotificationRecipients");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_NotificationId_UserId",
                table: "NotificationRecipients",
                columns: new[] { "NotificationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_UserId",
                table: "NotificationRecipients",
                column: "UserId",
                filter: "[IsRead] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationRecipients_NotificationId_UserId",
                table: "NotificationRecipients");

            migrationBuilder.DropIndex(
                name: "IX_NotificationRecipients_UserId",
                table: "NotificationRecipients");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_UserId_IsRead_NotificationId",
                table: "NotificationRecipients",
                columns: new[] { "UserId", "IsRead", "NotificationId" });
        }
    }
}
