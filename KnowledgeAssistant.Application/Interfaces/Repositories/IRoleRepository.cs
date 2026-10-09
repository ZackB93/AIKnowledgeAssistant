using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Domain.Entities.Roles;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task AddAsync(Role role, CancellationToken ct);
        Task<Role?> GetByIdAsync(int id, CancellationToken ct);
        Task<RoleResponse?> GetResponseByIdAsync(int id, CancellationToken ct);
        Task<List<RoleResponse>> GetAllResponsesAsync(CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}