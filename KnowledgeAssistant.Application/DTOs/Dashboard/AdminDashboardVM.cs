namespace KnowledgeAssistant.Application.DTOs.Dashboard
{
    public class AdminDashboardVM
    {
        public int TotalUsers { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int TotalChats { get; set; }
        public int ChatsThisMonth { get; set; }
        public int TotalDocuments { get; set; }
        public double StorageUsedGb { get; set; }
        public int TotalNotificationsSent { get; set; }
        public double NotificationReadRate { get; set; }
        public List<MonthlyCount> ChatsPerMonth { get; set; } = new();
        public List<NewUserItem> NewestUsers { get; set; } = new();
        public List<ActivityItem> RecentActivity { get; set; } = new();

        public static AdminDashboardVM Sample() => new()
        {
            TotalUsers = 1200,
            NewUsersThisWeek = 34,
            TotalChats = 5400,
            ChatsThisMonth = 840,
            TotalDocuments = 3100,
            StorageUsedGb = 4.8,
            TotalNotificationsSent = 9800,
            NotificationReadRate = 0.64,
            ChatsPerMonth = MonthlyCount.LastMonths(620, 780, 710, 940, 1130, 840),
            NewestUsers = new()
            {
                new() { Id = Guid.NewGuid(), FullName = "Amelia Grant", Email = "amelia.grant@example.com", IsEnabled = true,  CreatedUtc = DateTime.UtcNow.AddMinutes(-10) },
                new() { Id = Guid.NewGuid(), FullName = "Tom Reyes",    Email = "tom.reyes@example.com",    IsEnabled = true,  CreatedUtc = DateTime.UtcNow.AddHours(-5) },
                new() { Id = Guid.NewGuid(), FullName = "Priya Nair",   Email = "priya.nair@example.com",   IsEnabled = false, CreatedUtc = DateTime.UtcNow.AddDays(-1) },
            },
            RecentActivity = new()
            {
                new() { Text = "Amelia Grant signed up",            Kind = FeedKind.Success, OccurredUtc = DateTime.UtcNow.AddMinutes(-10) },
                new() { Text = "Tom Reyes started a new AI chat",   Kind = FeedKind.Info,    OccurredUtc = DateTime.UtcNow.AddMinutes(-40) },
                new() { Text = "Priya Nair's account was disabled", Kind = FeedKind.Warning, OccurredUtc = DateTime.UtcNow.AddHours(-2) },
            }
        };

    }

    public class NewUserItem
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsEnabled { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public class ActivityItem
    {
        public string Text { get; set; } = "";
        public FeedKind Kind { get; set; }
        public DateTime OccurredUtc { get; set; }
    }
}
