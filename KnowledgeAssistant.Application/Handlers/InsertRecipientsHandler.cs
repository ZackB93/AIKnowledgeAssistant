using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Notifications;
using KnowledgeAssistant.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Handlers
{
    public class InsertRecipientsChunkHandler : IMessageHandler<InsertRecipientsChunkMessage>
    {
        private readonly INotificationRepository _notificationRepository;

        public InsertRecipientsChunkHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task HandleAsync(InsertRecipientsChunkMessage message, CancellationToken ct)
        {
            await _notificationRepository.AddRecipientsChunkAsync(message.NotificationId, message.UserIds, ct);
        }
    }
}
