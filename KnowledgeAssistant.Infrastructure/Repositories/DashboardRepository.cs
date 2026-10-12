
using KnowledgeAssistant.Application.DTOs.Dashboard;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Chat;
using KnowledgeAssistant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly KnowledgeContext _context;

        public DashboardRepository(KnowledgeContext context)
        {
            _context = context;
        }

        public async Task<UserDashboardVM> GetUserDashboardDataAsync(int userId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var chartStart = monthStart.AddMonths(-5);

            var chats = _context.ChatSessions
                .AsNoTracking()
                .Where(c => c.UserId == userId);

            var vm = new UserDashboardVM
            {
                UnreadNotifications = await _context.NotificationRecipients
                    .CountAsync(n => n.UserId == userId && !n.IsRead, ct),

                TotalChats = await chats.CountAsync(ct),

                ChatsThisMonth = await chats
                    .CountAsync(c => c.CreatedAt >= monthStart, ct),

                TotalDocuments = await _context.Documents
                    .AsNoTracking()
                    .CountAsync(d => d.UserId == userId, ct),

                ChatsPerMonth = await ChatsPerMonthAsync(chats, chartStart, ct)
            };

            vm.RecentChats = await chats
                .OrderByDescending(c => c.UpdatedAt)
                .Take(5)
                .Select(c => new RecentChatItem
                {
                    Id = c.Id,
                    Title = c.Title,
                    LastActivityUtc = c.UpdatedAt,
                    LastQuestion = c.Messages
                        .Where(m => m.Role == ChatRole.User.ToString())
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefault() ?? ""
                })
                .ToListAsync(ct);

            vm.RecentNotifications = await _context.NotificationRecipients
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DeliveredAt)
                .Take(5)
                .Select(n => new RecentNotificationItem
                {
                    Id = n.Id,
                    IsRead = n.IsRead,
                    CreatedUtc = n.DeliveredAt,
                    Kind = FeedKind.Info
                })
                .ToListAsync(ct);

            return vm;
        }

        public async Task<AdminDashboardVM> GetAdminDashboardDataAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var chartStart = monthStart.AddMonths(-5);
            var weekAgo = now.AddDays(-7);

            var chats = _context.ChatSessions.AsNoTracking();
            var users = _context.Users.AsNoTracking();
            var recipients = _context.NotificationRecipients.AsNoTracking();
            var documents = _context.Documents.AsNoTracking();

            // Aggregate notification statistics in a single query.
            var notificationStats = await recipients
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Read = g.Count(n => n.IsRead)
                })
                .FirstOrDefaultAsync(ct);

            var totalNotifications = notificationStats?.Total ?? 0;
            var readNotifications = notificationStats?.Read ?? 0;

            // Calculate total document storage in bytes.
            var storageBytes = await documents
                .SumAsync(d => (long?)d.SizeBytes, ct) ?? 0;

            var vm = new AdminDashboardVM
            {
                TotalUsers = await users.CountAsync(ct),

                NewUsersThisWeek = await users
                    .CountAsync(u => u.CreatedDateTime >= weekAgo, ct),

                TotalChats = await chats.CountAsync(ct),

                ChatsThisMonth = await chats
                    .CountAsync(c => c.CreatedAt >= monthStart, ct),

                TotalDocuments = await documents.CountAsync(ct),

                StorageUsedGb = Math.Round(
                    storageBytes / 1024.0 / 1024.0 / 1024.0, 1),

                TotalNotificationsSent = totalNotifications,

                NotificationReadRate = totalNotifications == 0
                    ? 0
                    : (double)readNotifications / totalNotifications,

                ChatsPerMonth = await ChatsPerMonthAsync(chats, chartStart, ct)
            };

            vm.NewestUsers = await users
                .OrderByDescending(u => u.CreatedDateTime)
                .Take(5)
                .Select(u => new NewUserItem
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    Email = u.Credentials.EmailAddress,
                    IsEnabled = u.Enabled,
                    CreatedUtc = u.CreatedDateTime
                })
                .ToListAsync(ct);

            var signups = await users
                .OrderByDescending(u => u.CreatedDateTime)
                .Take(5)
                .Select(u => new ActivityItem
                {
                    Text = u.FirstName + " " + u.LastName + " signed up",
                    Kind = FeedKind.Success,
                    OccurredUtc = u.CreatedDateTime
                })
                .ToListAsync(ct);

            var newChats = await (
                from c in chats
                join u in users on c.UserId equals u.Id
                orderby c.CreatedAt descending
                select new ActivityItem
                {
                    Text = u.FirstName + " " + u.LastName + " started a new AI chat",
                    Kind = FeedKind.Info,
                    OccurredUtc = c.CreatedAt
                })
                .Take(5)
                .ToListAsync(ct);

            vm.RecentActivity = signups
                .Concat(newChats)
                .OrderByDescending(a => a.OccurredUtc)
                .Take(5)
                .ToList();

            return vm;
        }

        private static async Task<List<MonthlyCount>> ChatsPerMonthAsync(
            IQueryable<ChatSession> chats, DateTime from, CancellationToken ct)
        {
            var grouped = await chats
                .Where(c => c.CreatedAt >= from)
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync(ct);

            // Include months with no chats.
            var countsByMonth = grouped.ToDictionary(
                g => (g.Year, g.Month),
                g => g.Count);

            var counts = Enumerable.Range(0, 6)
                .Select(i => from.AddMonths(i))
                .Select(m => countsByMonth.GetValueOrDefault((m.Year, m.Month), 0))
                .ToArray();

            return MonthlyCount.LastMonths(counts);
        }
    }
}
