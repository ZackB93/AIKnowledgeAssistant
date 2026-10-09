using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Application.Services.Communication;

namespace KnowledgeAssistant.Application.Handlers;
public interface IMessageHandler<T>
{
    Task HandleAsync(T message, CancellationToken ct = default);
}

public class SendEmailHandler: IMessageHandler<SendEmailMessage>
{
    private readonly IEmailService _emailService;

    public SendEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(SendEmailMessage message, CancellationToken ct = default)
    {
        await _emailService.SendEmailAsync(message.EmailId, ct);
    }
}