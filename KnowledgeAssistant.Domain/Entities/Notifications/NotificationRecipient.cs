using KnowledgeAssistant.Domain.Entities.Users;
using KnowledgeAssistant.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Domain.Entities.Notifications
{
    public class NotificationRecipient
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public bool IsRead { get; set; }
        public DateTime DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public Notification Notification { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
