namespace KnowledgeAssistant.Application.DTOs.Dashboard
{
    public class UserDashboardVM
    {
        public int UnreadNotifications { get; set; }
        public int TotalChats { get; set; }
        public int ChatsThisMonth { get; set; }
        public int TotalDocuments { get; set; }
        public List<MonthlyCount> ChatsPerMonth { get; set; } = new();
        public List<RecentChatItem> RecentChats { get; set; } = new();
        public List<RecentNotificationItem> RecentNotifications { get; set; } = new();

        public static UserDashboardVM Sample() => new()
        {
            UnreadNotifications = 3,
            TotalChats = 41,
            ChatsThisMonth = 3,
            TotalDocuments = 14,
            ChatsPerMonth = MonthlyCount.LastMonths(6, 9, 4, 11, 8, 3),
            RecentChats = new()
            {
                new() { Id = Guid.NewGuid(), Title = "Leave policy summary", LastQuestion = "What is the notice period?",       LastActivityUtc = DateTime.UtcNow.AddMinutes(-2) },
                new() { Id = Guid.NewGuid(), Title = "Q3 report review",     LastQuestion = "Which region grew fastest?",       LastActivityUtc = DateTime.UtcNow.AddHours(-1) },
                new() { Id = Guid.NewGuid(), Title = "Supplier contract",    LastQuestion = "Summarise the termination clause", LastActivityUtc = DateTime.UtcNow.AddDays(-1) },
            },
            RecentNotifications = new()
            {
                new() { Id = Guid.NewGuid(), Text = "Handbook.pdf finished processing",   Kind = FeedKind.Success, CreatedUtc = DateTime.UtcNow.AddMinutes(-5), IsRead = false },
                new() { Id = Guid.NewGuid(), Text = "Q3-report.docx finished processing", Kind = FeedKind.Success, CreatedUtc = DateTime.UtcNow.AddHours(-1),   IsRead = false },
                new() { Id = Guid.NewGuid(), Text = "Storage is 75% full",                Kind = FeedKind.Warning, CreatedUtc = DateTime.UtcNow.AddDays(-1),    IsRead = false },
            }
        };
    }

    public class RecentChatItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string LastQuestion { get; set; } = "";
        public DateTime LastActivityUtc { get; set; }
    }

    public class RecentNotificationItem
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = "";
        public FeedKind Kind { get; set; }
        public DateTime CreatedUtc { get; set; }
        public bool IsRead { get; set; }
    }
}
