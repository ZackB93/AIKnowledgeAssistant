using KnowledgeAssistant.Application.DTOs.Emails;
using KnowledgeAssistant.Domain.Entities.Emails;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IEmailRepository
    {
        Task AddAsync(Email email, CancellationToken cancellationToken = default);
        Task<Email?> GetByIdAsync(int emailId, CancellationToken cancellationToken = default);
        Task<EmailResponse?> GetResponseByIdAsync(int emailId, CancellationToken cancellationToken = default);
        Task<List<EmailResponse>> GetResponsesByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<(List<EmailResponse> Items, int TotalCount)> GetPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<(List<EmailResponse> Items, int TotalCount)> SearchPaginatedAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}