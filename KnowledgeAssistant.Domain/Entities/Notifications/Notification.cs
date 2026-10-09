using KnowledgeAssistant.Domain.Entities.Users;
using KnowledgeAssistant.Domain.Enums;

namespace KnowledgeAssistant.Domain.Entities.Notifications
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        public ICollection<NotificationRecipient> Recipients { get; set; } = [];
    }
}
