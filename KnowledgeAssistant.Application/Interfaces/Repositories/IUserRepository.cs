using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Domain.Entities.Users;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task<User?> GetByIdWithCredentialsAndRolesAsync(int id, CancellationToken cancellationToken = default);
        Task<SignInDetails?> GetSignInDetailsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetResponseByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<(List<UserResponse> Items, int TotalCount)> GetPagedResponsesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<(List<UserResponse> Items, int TotalCount)> SearchPagedResponsesAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task UpdatePasswordHashAsync(int userId, string newHash, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}