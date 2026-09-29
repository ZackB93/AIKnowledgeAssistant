using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeAssistant.Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailUserIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Emails_Status_CreatedAt",
                table: "Emails");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Status_CreatedAt_UserId",
                table: "Emails",
                columns: new[] { "Status", "CreatedAt", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Emails_Status_CreatedAt_UserId",
                table: "Emails");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Status_CreatedAt",
                table: "Emails",
                columns: new[] { "Status", "CreatedAt" });
        }
    }
}
