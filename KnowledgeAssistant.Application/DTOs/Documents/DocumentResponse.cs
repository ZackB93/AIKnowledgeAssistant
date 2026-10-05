using KnowledgeAssistant.Application.Entities.Documents;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Documents
{
    public class DocumentResponse
    {
        public int Id { get; set; }

        public int? ChatSessionId { get; set; }

        public string FileName { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long SizeBytes { get; set; }

        public DocumentStatus Status { get; set; }

        public int ChunkCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
