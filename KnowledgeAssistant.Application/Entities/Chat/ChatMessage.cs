using System;
using System.Collections.Generic;
using System.Text;
using KnowledgeAssistant.Application.Entities.Documents;

namespace KnowledgeAssistant.Application.Entities.Chat
{
    public class ChatMessage
    {
        public long Id { get; set; }

        public int ChatSessionId { get; set; }

        public string Role { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public ChatSession ChatSession { get; set; } = null!;

        public ICollection<Document> Attachments { get; set; } = [];
    }
}
