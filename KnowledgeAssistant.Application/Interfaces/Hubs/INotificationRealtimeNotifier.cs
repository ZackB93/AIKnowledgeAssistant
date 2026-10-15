namespace KnowledgeAssistant.Application.Interfaces.Hubs
{
    public interface INotificationRealtimeNotifier
    {
        Task SendUnreadCountAsync(int userId, int unreadCount, CancellationToken ct = default);
    }
}
