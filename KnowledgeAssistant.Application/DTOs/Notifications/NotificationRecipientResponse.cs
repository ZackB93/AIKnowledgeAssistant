using KnowledgeAssistant.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Notifications
{
    public class NotificationRecipientResponse
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime DeliveredAt { get; set; }
    }
}
