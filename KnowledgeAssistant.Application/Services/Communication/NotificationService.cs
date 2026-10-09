using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Notifications;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Notifications;
using Microsoft.Extensions.Configuration;

namespace KnowledgeAssistant.Application.Services.Communication
{
    public interface INotificationService
    {
        Task SendNotificationAsync(CreateNotificationRequest request, CancellationToken ct);
        Task<NotificationResponse?> GetNotificationByIdAsync(int notificationId, CancellationToken ct);
        Task<PaginatedResponse<NotificationUserSummaryResponse>> GetNotificationsByUserIdAsync(int userId, int pageNumber, int pageSize, CancellationToken ct);
        Task<List<NotificationRecipientResponse>> GetNotificationRecipientsAsync(int notificationId, CancellationToken ct);
        Task<PaginatedResponse<NotificationSummaryResponse>> GetNotificationsAsync(int pageNumber, int pageSize, CancellationToken ct);
        Task<PaginatedResponse<NotificationSummaryResponse>> SearchNotificationsAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken ct);
    }

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository, IConfiguration configuration)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task SendNotificationAsync(CreateNotificationRequest request, CancellationToken ct)
        {
            var notification = new Notification
            {
                Title = request.Title,
                Body = request.Body,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = request.CreatedByUserId,
                Recipients = request.UserIds.Distinct().Select(userId => new NotificationRecipient
                {
                    UserId = userId,
                    IsRead = false,
                    DeliveredAt = DateTime.UtcNow
                }).ToList()
            };

            await _notificationRepository.AddAsync(notification, ct);
            await _notificationRepository.SaveChangesAsync(ct);
        }

        public async Task<NotificationResponse?> GetNotificationByIdAsync(int notificationId, CancellationToken ct)
        {
            return await _notificationRepository.GetResponseByIdAsync(notificationId, ct);
        }

        public async Task<PaginatedResponse<NotificationUserSummaryResponse>> GetNotificationsByUserIdAsync(int userId, int pageNumber, int pageSize, CancellationToken ct)
        {
            var (pageNumberValid, pageSizeValid) = NormalizePagination(pageNumber, pageSize);

            var (items, totalCount) = await _notificationRepository.GetPaginatedByUserIdAsync(userId, pageNumberValid, pageSizeValid, ct);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSizeValid);

            return new PaginatedResponse<NotificationUserSummaryResponse>
            {
                Items = items,
                PageNumber = pageNumberValid,
                PageSize = pageSizeValid,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<List<NotificationRecipientResponse>> GetNotificationRecipientsAsync(int notificationId, CancellationToken ct)
        {
            return await _notificationRepository.GetRecipientsResponseAsync(notificationId, ct);
        }

        public async Task<PaginatedResponse<NotificationSummaryResponse>> GetNotificationsAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            var (pageNumberValid, pageSizeValid) = NormalizePagination(pageNumber, pageSize);

            var (items, totalCount) = await _notificationRepository.GetPaginatedAsync(pageNumberValid, pageSizeValid, ct);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSizeValid);

            return new PaginatedResponse<NotificationSummaryResponse>
            {
                Items = items,
                PageNumber = pageNumberValid,
                PageSize = pageSizeValid,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<PaginatedResponse<NotificationSummaryResponse>> SearchNotificationsAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken ct)
        {
            var (pageNumberValid, pageSizeValid) = NormalizePagination(pageNumber, pageSize);

            var (items, totalCount) = await _notificationRepository.SearchPaginatedAsync(searchTerm, pageNumberValid, pageSizeValid, ct);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSizeValid);

            return new PaginatedResponse<NotificationSummaryResponse>
            {
                Items = items,
                PageNumber = pageNumberValid,
                PageSize = pageSizeValid,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        private static (int PageNumber, int PageSize) NormalizePagination(int pageNumber, int pageSize)
        {
            const int maxPageSize = 50;
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            return (pageNumber, pageSize);
        }
    }
}
