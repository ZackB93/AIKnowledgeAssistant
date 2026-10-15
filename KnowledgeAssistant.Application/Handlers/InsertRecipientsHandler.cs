using KnowledgeAssistant.Application.Interfaces.Hubs;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Notifications;

namespace KnowledgeAssistant.Application.Handlers
{
    public class InsertRecipientsChunkHandler : IMessageHandler<InsertRecipientsChunkMessage>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationRealtimeNotifier _notificationRealtimeNotifier;


        public InsertRecipientsChunkHandler(INotificationRepository notificationRepository, INotificationRealtimeNotifier notificationRealtimeNotifier)
        {
            _notificationRepository = notificationRepository;
            _notificationRealtimeNotifier = notificationRealtimeNotifier;
        }

        public async Task HandleAsync(InsertRecipientsChunkMessage message, CancellationToken ct)
        {
            await _notificationRepository.AddRecipientsChunkAsync(message.NotificationId, message.UserIds, ct);

            foreach (var userId in message.UserIds.Distinct())
            {
                var unread = await _notificationRepository.GetUnreadNotificationCountByUserId(userId, ct);
                await _notificationRealtimeNotifier.SendUnreadCountAsync(userId, unread.Count, ct);
            }
        }
    }
}
