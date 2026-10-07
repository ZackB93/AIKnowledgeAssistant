using KnowledgeAssistant.Domain.Entities.Chat;
using KnowledgeAssistant.Domain.Entities.Users;
using KnowledgeAssistant.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Domain.Entities.Documents
{
    public class Document
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int? ChatSessionId { get; set; }

        public long? ChatMessageId { get; set; }

        public string FileName { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long SizeBytes { get; set; }

        public DocumentStatus Status { get; set; }

        public string? ErrorMessage { get; set; }

        public string? EmbeddingModel { get; set; }

        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;

        public ChatSession? ChatSession { get; set; }

        public ChatMessage? ChatMessage { get; set; }

        public ICollection<DocumentChunk> Chunks { get; set; } = [];
    }

}
