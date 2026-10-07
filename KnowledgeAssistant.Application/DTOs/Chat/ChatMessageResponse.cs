using KnowledgeAssistant.Domain.Entities.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Chat
{
    public class ChatMessageResponse
    {
        public long Id { get; set; }

        public int ChatSessionId { get; set; }

        public string Role { get; set; } = null!;

        public string Content { get; set; } = null!;

        public string? NewTitle { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
