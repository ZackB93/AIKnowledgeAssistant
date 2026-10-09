using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Domain.Entities.Notifications
{
    public class FanOutNotificationMessage
    {
        public int NotificationId { get; set; }
        public List<int> UserIds { get; set; } = new();
    }
}
