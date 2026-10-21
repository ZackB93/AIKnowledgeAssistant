using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeAssistant.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMemoryTableConig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserMemory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MemoryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceChatSessionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMemory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMemory_SourceChatSessionId",
                table: "UserMemory",
                column: "SourceChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMemory_UserId_MemoryType",
                table: "UserMemory",
                columns: new[] { "UserId", "MemoryType" });

            migrationBuilder.CreateIndex(
                name: "IX_UserMemory_UserId_NormalizedKey",
                table: "UserMemory",
                columns: new[] { "UserId", "NormalizedKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMemory");
        }
    }
}
