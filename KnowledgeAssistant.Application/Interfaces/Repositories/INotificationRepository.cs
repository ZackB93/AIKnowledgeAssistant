using KnowledgeAssistant.Application.DTOs.Notifications;
using KnowledgeAssistant.Domain.Entities.Notifications;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification, CancellationToken ct);
        Task<Notification?> GetByIdAsync(int notificationId, CancellationToken ct);
        Task<NotificationResponse?> GetResponseByIdAsync(int notificationId, CancellationToken ct);
        Task<(List<NotificationSummaryResponse> items, int totalCount)> GetPaginatedAsync(int pageNumber, int pageSize, CancellationToken ct);
        Task<(List<NotificationSummaryResponse> items, int totalCount)> SearchPaginatedAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
