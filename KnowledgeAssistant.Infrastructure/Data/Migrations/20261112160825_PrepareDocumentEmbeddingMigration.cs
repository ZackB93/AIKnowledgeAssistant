using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeAssistant.Infrastructure.Data.Migrations
{
    public partial class PrepareDocumentEmbeddingMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                EXEC sp_rename
                    'dbo.DocumentChunks.Embedding',
                    'LegacyEmbedding',
                    'COLUMN';

                ALTER TABLE dbo.DocumentChunks
                ADD Embedding VECTOR(1536) NULL;

                UPDATE dbo.DocumentChunks
                SET Embedding = Embedding;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE dbo.DocumentChunks
                DROP COLUMN Embedding;

                EXEC sp_rename
                    'dbo.DocumentChunks.LegacyEmbedding',
                    'Embedding',
                    'COLUMN';
                """);
        }
    }
}
