using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Chat
{
    public class AddChatMessageRequest
    {
        public int ChatSessionId { get; set; }
        public string Content { get; set; } = null!;
    }
}
