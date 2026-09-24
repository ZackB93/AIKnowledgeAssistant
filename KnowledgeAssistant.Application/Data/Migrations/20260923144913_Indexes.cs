using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeAssistant.Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_FirstName",
                table: "Users",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastName",
                table: "Users",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastName_FirstName",
                table: "Users",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCredentials_EmailAddress",
                table: "UserCredentials",
                column: "EmailAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_FirstName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LastName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LastName_FirstName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserCredentials_EmailAddress",
                table: "UserCredentials");
        }
    }
}
