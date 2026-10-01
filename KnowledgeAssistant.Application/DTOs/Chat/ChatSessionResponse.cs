using KnowledgeAssistant.Application.Entities.Chat;
using KnowledgeAssistant.Application.Entities.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Chat
{
    public class ChatSessionResponse
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<ChatMessageResponse> Messages { get; set; } = new();
    }
}
