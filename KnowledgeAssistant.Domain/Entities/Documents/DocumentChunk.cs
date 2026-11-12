using Microsoft.Data.SqlTypes;

namespace KnowledgeAssistant.Domain.Entities.Documents
{
    public class DocumentChunk
    {
        public long Id { get; set; }

        public int DocumentId { get; set; }

        public int ChunkIndex { get; set; }

        public string Content { get; set; } = null!;

        public SqlVector<float> Embedding { get; set; }

        public Document Document { get; set; } = null!;
    }
}
