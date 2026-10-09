using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Emails;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Application.Services.Infrastructure;
using KnowledgeAssistant.Domain.Entities.Emails;
using Microsoft.Extensions.Configuration;
using Resend;
using EmailStatus = KnowledgeAssistant.Domain.Enums.EmailStatus;

namespace KnowledgeAssistant.Application.Services.Communication
{
    public interface IEmailService
    {
        Task QueueEmailAsync(int userId, string to, string subject, string body, CancellationToken ct, bool isHtml = true);
        Task SendEmailAsync(int emailId, CancellationToken ct);
        Task<EmailResponse?> GetEmailByIdAsync(int emailId, CancellationToken ct);
        Task<List<EmailResponse>> GetEmailsByUserIdAsync(int userId, CancellationToken ct);
        Task<PaginatedResponse<EmailResponse>> GetEmailsAsync(int pageNumber, int pageSize, CancellationToken ct);
        Task<PaginatedResponse<EmailResponse>> SearchEmailsAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken ct);
    }

    public class EmailService : IEmailService
    {
        private readonly IEmailRepository _emailRepository;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly IResend _resendService;
        private readonly bool _rabbitMQEnabled;

        public EmailService(
            IEmailRepository emailRepository,
            IRabbitMQService rabbitMQService,
            IResend resendService,
            IConfiguration configuration)
        {
            _emailRepository = emailRepository;
            _rabbitMQService = rabbitMQService;
            _resendService = resendService;
            _rabbitMQEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled");
        }

        public async Task QueueEmailAsync(int userId, string to, string subject, string body, CancellationToken ct, bool isHtml = true)
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

            await _emailRepository.AddAsync(email, ct);
            await _emailRepository.SaveChangesAsync(ct);

            if (_rabbitMQEnabled)
            {
                await _rabbitMQService.PublishAsync(new SendEmailMessage { EmailId = email.Id }, "emails", ct);
            }
            else
            {
                await SendEmailAsync(email.Id, ct);
            }
        }

        public async Task SendEmailAsync(int emailId, CancellationToken ct)
        {
            var email = await _emailRepository.GetByIdAsync(emailId, ct);

            if (email == null)
            {
                throw new InvalidOperationException($"Email {emailId} was not found.");
            }

            email.Status = EmailStatus.Processing;
            await _emailRepository.SaveChangesAsync(ct);

            try
            {
                await _resendService.EmailSendAsync(new EmailMessage
                {
                    From = "onboarding@resend.dev",
                    To = "zackzack93@hotmail.com",
                    Subject = email.Subject,
                    HtmlBody = email.Body
                }, ct);

                email.Status = EmailStatus.Sent;
                email.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                email.Status = EmailStatus.Failed;
                email.FailedAt = DateTime.UtcNow;
                email.ErrorMessage = ex.Message;
                email.RetryCount++;
            }

            await _emailRepository.SaveChangesAsync(ct);
        }

        public Task<EmailResponse?> GetEmailByIdAsync(int emailId, CancellationToken ct)
        {
            return _emailRepository.GetResponseByIdAsync(emailId, ct);
        }

        public Task<List<EmailResponse>> GetEmailsByUserIdAsync(int userId, CancellationToken ct)
        {
            return _emailRepository.GetResponsesByUserIdAsync(userId, ct);
        }

        public async Task<PaginatedResponse<EmailResponse>> GetEmailsAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            var (pageNumberValid, pageSizeValid) = NormalizePagination(pageNumber, pageSize);

            var (items, totalCount) = await _emailRepository.GetPaginatedAsync(pageNumberValid, pageSizeValid, ct);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSizeValid);

            return new PaginatedResponse<EmailResponse>
            {
                Items = items,
                PageNumber = pageNumberValid,
                PageSize = pageSizeValid,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<PaginatedResponse<EmailResponse>> SearchEmailsAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken ct)
        {
            var (pageNumberValid, pageSizeValid) = NormalizePagination(pageNumber, pageSize);

            var (items, totalCount) = await _emailRepository.SearchPaginatedAsync(searchTerm, pageNumberValid, pageSizeValid, CancellationToken.None);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSizeValid);

            return new PaginatedResponse<EmailResponse>
            {
                Items = items,
                PageNumber = pageNumberValid,
                PageSize = pageSizeValid,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        private static (int PageNumber, int PageSize) NormalizePagination(int pageNumber, int pageSize)
        {
            const int maxPageSize = 50;
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            return (pageNumber, pageSize);
        }
    }
}