using KnowledgeAssistant.Domain.Enums;

namespace KnowledgeAssistant.Domain.Entities.Audit
{
    public class ActivityAudit
    {
        public long Id { get; set; }
        public DateTime DateTime { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public AuditAction Action { get; set; }
        public string? Description { get; set; }
        public string EntityName { get; set; } = null!;
        public string? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangedProperties { get; set; }
    }
}
