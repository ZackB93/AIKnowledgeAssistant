using KnowledgeAssistant.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Domain.Entities.Notifications
{
    public class NotificationSummaryResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public int RecipientCount { get; set; }
        public int ReadCount { get; set; }
    }
}
