using KnowledgeAssistant.Domain.Entities.Audit;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IAuditRepository
    {
        Task AddActivityAsync(ActivityAudit activity, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
