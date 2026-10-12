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
    }

    public class NewUserItem
    {
        public int Id { get; set; }
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
