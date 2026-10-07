using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Domain.Entities.Roles;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task AddAsync(Role role, CancellationToken cancellationToken = default);
        Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<RoleResponse?> GetResponseByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<RoleResponse>> GetAllResponsesAsync(CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}