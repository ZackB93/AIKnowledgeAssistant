using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Audit;
using KnowledgeAssistant.Infrastructure.Data.Context;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly KnowledgeContext _context;

        public AuditRepository(KnowledgeContext context)
        {
            _context = context;
        }

        public async Task AddActivityAsync(ActivityAudit activity, CancellationToken ct)
        {
            await _context.ActivityAudit.AddAsync(activity, ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }
  
    }
}