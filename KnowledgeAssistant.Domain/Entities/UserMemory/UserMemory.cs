using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Domain.Entities.UserMemory
{
    public class UserMemory
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string MemoryType { get; set; } = string.Empty;

        public string NormalizedKey { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public int? SourceChatSessionId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
