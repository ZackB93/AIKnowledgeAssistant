using KnowledgeAssistant.Application.Interfaces.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KnowledgeAssistant.API.Hubs
{
    public sealed class NotificationRealtimeNotifier(IHubContext<NotificationHub> hubContext) : INotificationRealtimeNotifier
    {
        public Task SendUnreadCountAsync(int userId, int unreadCount, CancellationToken ct = default)
        {
            return hubContext.Clients.User(userId.ToString()).SendAsync("UnreadCountUpdated", unreadCount, ct);
        }
    }
}
