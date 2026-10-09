using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Notifications;
using KnowledgeAssistant.Application.Handlers;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Application.Services.Infrastructure;
using KnowledgeAssistant.Domain.Entities.Notifications;
using KnowledgeAssistant.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace KnowledgeAssistant.Application.Services.Communication
{
    public interface INotificationService
    {
        Task<NotificationResponse> SendNotificationAsync(CreateNotificationRequest request, CancellationToken ct);
        Task<NotificationResponse?> GetNotificationByIdAsync(int notificationId, CancellationToken ct);
        Task<PaginatedResponse<NotificationUserSummaryResponse>> GetNotificationsByUserIdAsync(int userId, int pageNumber, int pageSize, CancellationToken ct);
        Task<List<NotificationRecipientResponse>> GetNotificationRecipientsAsync(int notificationId, CancellationToken ct);
        Task<PaginatedResponse<NotificationSummaryResponse>> GetNotificationsAsync(int pageNumber, int pageSize, CancellationToken ct);
        Task<PaginatedResponse<NotificationSummaryResponse>> SearchNotificationsAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken ct);
    }

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly bool _rabbitMQEnabled;

        public NotificationService(
            INotificationRepository notificationRepository,
            IRabbitMQService rabbitMQService,
            IConfiguration configuration)
        {
            _notificationRepository = notificationRepository;
            _rabbitMQService = rabbitMQService;
            _rabbitMQEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled");

        }

        public async Task<NotificationResponse> SendNotificationAsync(CreateNotificationRequest request, CancellationToken ct)
        {
            var userIds = request.UserIds.Distinct().ToList();

            var notification = new Notification
            {
                Title = request.Title,
                Body = request.Body,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = request.CreatedByUserId,
                Status = userIds.Count == 0 ? NotificationStatus.Completed : NotificationStatus.Queued,
                TotalRecipients = userIds.Count
            };

            await _notificationRepository.AddAsync(notification, ct);
            await _notificationRepository.SaveChangesAsync(ct);

            var chunks = userIds
                .Chunk(1000)
                .Select(chunk => new InsertRecipientsChunkMessage
                {
                    NotificationId = notification.Id,
                    UserIds = chunk.ToList()
                })
                .ToList();

            try
            {
                if (_rabbitMQEnabled)
                {
                    await _rabbitMQService.PublishBatchAsync(chunks, "notification-recipients", ct);
                }
                else
                {
                    foreach (var chunk in chunks)
                    {
                        await _notificationRepository.AddRecipientsChunkAsync(chunk.NotificationId, chunk.UserIds, ct);
                    }
                }
            }
            catch
            {
                notification.Status = NotificationStatus.Failed;
                await _notificationRepository.SaveChangesAsync(ct);
            }

            return new NotificationResponse
            {
                Id = notification.Id,
                Title = notification.Title,
                Body = notification.Body,
                CreatedAt = notification.CreatedAt,
                CreatedByUserId = (int)notification.CreatedByUserId,
                Status = notification.Status,
                TotalRecipients = notification.TotalRecipients
            };
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
