using KnowledgeAssistant.Application.Entities.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Entities.Chat
{
    public class ChatSession
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;

        public ICollection<ChatMessage> Messages { get; set; } = [];
    }
}
