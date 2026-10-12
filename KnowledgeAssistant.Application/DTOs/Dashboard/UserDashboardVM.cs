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
    }

    public class RecentChatItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string LastQuestion { get; set; } = "";
        public DateTime LastActivityUtc { get; set; }
    }

    public class RecentNotificationItem
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";
        public FeedKind Kind { get; set; }
        public DateTime CreatedUtc { get; set; }
        public bool IsRead { get; set; }
    }
}
