using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Documents
{
    public class DocumentSearchResult
    {
        public int DocumentId { get; set; }
        public long ChunkId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public double Similarity { get; set; }
    }
}
