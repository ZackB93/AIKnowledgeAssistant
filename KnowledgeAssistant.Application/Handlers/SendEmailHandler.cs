using KnowledgeAssistant.Application.Entities.Emails;
using KnowledgeAssistant.Application.Services;
using static KnowledgeAssistant.API.BackgroundServices.RabbitMQBackgroundService;

namespace KnowledgeAssistant.Application.Messaging.Handlers;

public class SendEmailHandler: IMessageHandler<SendEmailMessage>
{
    private readonly IEmailService _emailService;

    public SendEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(SendEmailMessage message,CancellationToken cancellationToken = default)
    {
        await _emailService.SendEmailAsync(message.EmailId,cancellationToken);
    }
}