using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.Emails;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Entities.Emails;
using KnowledgeAssistant.Application.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Resend;
using EmailStatus = KnowledgeAssistant.Application.Entities.Emails.EmailStatus;

namespace KnowledgeAssistant.Application.Services
{
    public interface IEmailService
    {
        Task QueueEmailAsync(int userId, string to, string subject, string body, bool isHtml = true);
        Task SendEmailAsync(int emailId, CancellationToken cancellationToken = default);
        Task<EmailResponse?> GetEmailByIdAsync(int emailId);
        Task<List<EmailResponse>> GetEmailsByUserIdAsync(int userId);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly KnowledgeContext _context;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly bool _rabbitMQEnabled;
        private IResend _resendService;
        
        public EmailService(KnowledgeContext context, IRabbitMQService rabbitMQService, IResend resendService, IConfiguration configuration)
        {
            _configuration = configuration;
            _context = context;
            _rabbitMQService = rabbitMQService;
            _resendService = resendService;
            _rabbitMQEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        }

        public async Task QueueEmailAsync(int userId, string to, string subject, string body, bool isHtml = true)
        {
            var email = new Email
            {
                UserId = userId,
                To = to,
                Subject = subject,
                Body = body,
                IsHtml = isHtml,
                Status = EmailStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            _context.Emails.Add(email);

            await _context.SaveChangesAsync();

            if (_rabbitMQEnabled)
            {
                await _rabbitMQService.PublishAsync(email.Id, "emails");
            }
            else
            {
                await SendEmailAsync(email.Id);
            }
        }

        public async Task SendEmailAsync(int emailId, CancellationToken cancellationToken = default)
        {
            var email = await _context.Emails.FirstOrDefaultAsync(x => x.Id == emailId, cancellationToken);

            if (email == null)
            {
                throw new InvalidOperationException($"Email {emailId} was not found.");
            }

            email.Status = EmailStatus.Processing;

            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var response = await _resendService.EmailSendAsync(new EmailMessage
                {
                     From = "onboarding@resend.dev",
                     To = email.To,
                     Subject = email.Subject,
                     HtmlBody = email.IsHtml ? email.Body : null
                });

                email.Status = EmailStatus.Sent;
                email.SentAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                email.Status = EmailStatus.Failed;
                email.FailedAt = DateTime.UtcNow;
                email.ErrorMessage = ex.Message;
                email.RetryCount++;

                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<EmailResponse?> GetEmailByIdAsync(int emailId)
        {
            var email = await _context.Emails
                .AsNoTracking()
                .Select(x => new EmailResponse
                {
                    Id = x.Id,
                }).FirstOrDefaultAsync(x => x.Id == emailId);

            return email;
        }

        public async Task<List<EmailResponse>> GetEmailsByUserIdAsync(int userId)
        {
            var emails = await _context.Emails
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new EmailResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                }).ToListAsync();

            return emails;
        }
    }
}
