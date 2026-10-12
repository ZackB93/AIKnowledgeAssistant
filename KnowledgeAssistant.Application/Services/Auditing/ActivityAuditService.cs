using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Audit;

namespace KnowledgeAssistant.Application.Services.Auditing
{
    public interface IActivityAuditService
    {
        Task LogActivityAsync(ActivityAudit activity, CancellationToken ct);
    }

    public class ActivityAuditService : IActivityAuditService
    {
        private readonly IAuditRepository _auditRepository;

        public ActivityAuditService(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        public async Task LogActivityAsync(ActivityAudit activity, CancellationToken ct)
        {
            await _auditRepository.AddActivityAsync(activity, ct);
            await _auditRepository.SaveChangesAsync(ct);
        }
    }
}
