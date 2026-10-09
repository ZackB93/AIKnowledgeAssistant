using KnowledgeAssistant.Application.DTOs.Notifications;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Notifications;
using KnowledgeAssistant.Domain.Enums;
using KnowledgeAssistant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly KnowledgeContext _context;

        public NotificationRepository(KnowledgeContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification, CancellationToken ct)
        {
            await _context.Notifications.AddAsync(notification, ct);
        }

        public async Task<int> AddRecipientsChunkAsync(int notificationId, IReadOnlyCollection<int> userIds, CancellationToken ct)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Users who already have a row (from a previous delivery of this same message)
                var existing = await _context.NotificationRecipients
                    .AsNoTracking()
                    .Where(r => r.NotificationId == notificationId && userIds.Contains(r.UserId))
                    .Select(r => r.UserId)
                    .ToListAsync(ct);

                var now = DateTime.UtcNow;

                var newRows = userIds
                    .Except(existing)
                    .Select(userId => new NotificationRecipient
                    {
                        NotificationId = notificationId,
                        UserId = userId,
                        IsRead = false,
                        DeliveredAt = now
                    })
                    .ToList();

                await using var transaction = await _context.Database.BeginTransactionAsync(ct);

                if (newRows.Count > 0)
                {
                    _context.NotificationRecipients.AddRange(newRows);
                    await _context.SaveChangesAsync(ct);

                    await _context.Notifications
                        .Where(n => n.Id == notificationId)
                        .ExecuteUpdateAsync(s => s.SetProperty(
                            n => n.ProcessedRecipients,
                            n => n.ProcessedRecipients + newRows.Count), ct);
                }

                await _context.Notifications
                    .Where(n => n.Id == notificationId
                             && n.ProcessedRecipients >= n.TotalRecipients
                             && n.Status != NotificationStatus.Completed)
                    .ExecuteUpdateAsync(s => s.SetProperty(n => n.Status, NotificationStatus.Completed), ct);

                await transaction.CommitAsync(ct);

                // Detach so a long-lived/scoped context doesn't keep 1000 tracked entities around
                _context.ChangeTracker.Clear();

                return newRows.Count;
            });
        }

        public async Task<Notification?> GetByIdAsync(int notificationId, CancellationToken ct)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == notificationId, ct);
        }

        public async Task<List<NotificationRecipientResponse>> GetRecipientsResponseAsync(int notificationId, CancellationToken ct)
        {
            return await _context.NotificationRecipients
                .AsNoTracking()
                .Where(x => x.NotificationId == notificationId)
                .Select(x => new NotificationRecipientResponse
                {
                    Id = x.Id,
                    NotificationId = x.NotificationId,
                    UserId = x.UserId,
                    IsRead = x.IsRead,
                    ReadAt = x.ReadAt,
                    UserName = x.User.FirstName + " " + x.User.LastName,
                    DeliveredAt = x.DeliveredAt
                })
                .ToListAsync(ct);
        }

        public async Task<NotificationResponse?> GetResponseByIdAsync(int notificationId, CancellationToken ct)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(x => x.Id == notificationId)
                .Select(x => new NotificationResponse
                {
                    Id = x.Id,
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<(List<NotificationUserSummaryResponse> items, int totalCount)> GetPaginatedByUserIdAsync(int userId, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _context.NotificationRecipients
                    .AsNoTracking()
                    .Where(r => r.UserId == userId);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(r => r.Notification.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new NotificationUserSummaryResponse
                {
                    NotificationId = r.Notification.Id,
                    Title = r.Notification.Title,
                    Body = r.Notification.Body,
                    CreatedAt = r.Notification.CreatedAt,
                    CreatedByUserId = (int)r.Notification.CreatedByUserId,
                    CreatedByUserName = r.Notification.CreatedByUser.FirstName + " " + r.Notification.CreatedByUser.LastName,
                    Read = r.IsRead,
                    ReadDate = r.ReadAt
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<(List<NotificationSummaryResponse> items, int totalCount)> GetPaginatedAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            var totalCount = await _context.Notifications.CountAsync(ct);

            var items = await _context.Notifications
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new NotificationSummaryResponse
                {
                    Id = x.Id,
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<(List<NotificationSummaryResponse> items, int totalCount)> SearchPaginatedAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _context.Notifications.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    x.Title.Contains(searchTerm) ||
                    x.CreatedAt.ToString().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new NotificationSummaryResponse
                {
                    Id = x.Id,
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}