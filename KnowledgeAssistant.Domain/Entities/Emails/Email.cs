using KnowledgeAssistant.Domain.Entities.Users;
using KnowledgeAssistant.Domain.Enums;

namespace KnowledgeAssistant.Domain.Entities.Emails
{
    public class Email
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
        public EmailStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public User User { get; set; } = null!;
    }
}
