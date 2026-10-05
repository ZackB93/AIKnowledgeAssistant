using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Entities.Documents
{
    public class DocumentChunk
    {
        public long Id { get; set; }

        public int DocumentId { get; set; }

        public int ChunkIndex { get; set; }

        public string Content { get; set; } = null!;

        public byte[] Embedding { get; set; } = null!;

        public Document Document { get; set; } = null!;
    }
}
