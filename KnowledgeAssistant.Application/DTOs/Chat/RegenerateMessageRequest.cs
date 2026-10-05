using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Chat
{
    public  class RegenerateMessageRequest
    {
        public long ChatMessageId { get; set; }
        public int ChatSessionId { get; set; }
    }
}
