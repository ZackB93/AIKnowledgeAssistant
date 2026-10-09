using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeAssistant.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class NotificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationRecipients_UserId_IsRead",
                table: "NotificationRecipients");

            migrationBuilder.AddColumn<int>(
                name: "ProcessedRecipients",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRecipients",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_UserId_IsRead_NotificationId",
                table: "NotificationRecipients",
                columns: new[] { "UserId", "IsRead", "NotificationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationRecipients_UserId_IsRead_NotificationId",
                table: "NotificationRecipients");

            migrationBuilder.DropColumn(
                name: "ProcessedRecipients",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TotalRecipients",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_UserId_IsRead",
                table: "NotificationRecipients",
                columns: new[] { "UserId", "IsRead" });
        }
    }
}
