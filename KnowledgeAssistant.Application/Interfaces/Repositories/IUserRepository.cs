using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Domain.Entities.Users;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task AddAsync(User user, CancellationToken ct);
        Task<User?> GetByIdWithCredentialsAndRolesAsync(int id, CancellationToken ct);
        Task<SignInDetails?> GetSignInDetailsByEmailAsync(string email, CancellationToken ct);
        Task<UserResponse?> GetResponseByIdAsync(int id, CancellationToken ct);
        Task<(List<UserResponse> Items, int TotalCount)> GetPagedResponsesAsync(int pageNumber, int pageSize, CancellationToken ct);
        Task<(List<UserResponse> Items, int TotalCount)> SearchPagedResponsesAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
        Task UpdatePasswordHashAsync(int userId, string newHash, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}