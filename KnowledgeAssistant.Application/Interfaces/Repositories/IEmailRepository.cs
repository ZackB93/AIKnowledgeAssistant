using KnowledgeAssistant.Application.DTOs.Emails;
using KnowledgeAssistant.Domain.Entities.Emails;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IEmailRepository
    {
        Task AddAsync(Email email, CancellationToken ct);
        Task<Email?> GetByIdAsync(int emailId, CancellationToken ct);
        Task<EmailResponse?> GetResponseByIdAsync(int emailId, CancellationToken ct);
        Task<List<EmailResponse>> GetResponsesByUserIdAsync(int userId, CancellationToken ct);
        Task<(List<EmailResponse> Items, int TotalCount)> GetPaginatedAsync(int pageNumber, int pageSize, CancellationToken ct);
        Task<(List<EmailResponse> Items, int TotalCount)> SearchPaginatedAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}