using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Emails;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Resend;
using EmailStatus = KnowledgeAssistant.Domain.Enums.EmailStatus;

namespace KnowledgeAssistant.Application.Services
{
    public interface IEmailService
    {
        Task QueueEmailAsync(int userId, string to, string subject, string body, bool isHtml = true);
        Task SendEmailAsync(int emailId, CancellationToken cancellationToken = default);
        Task<EmailResponse?> GetEmailByIdAsync(int emailId);
        Task<List<EmailResponse>> GetEmailsByUserIdAsync(int userId);
        Task<PaginatedResponse<EmailResponse>> GetEmailsAsync(int pageNumber, int pageSize);
        Task<PaginatedResponse<EmailResponse>> SearchEmailsAsync(string searchTerm, int pageNumber, int pageSize);
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

            await _emailRepository.AddAsync(email);
            await _emailRepository.SaveChangesAsync();

            if (_rabbitMQEnabled)
            {
                await _rabbitMQService.PublishAsync(new SendEmailMessage { EmailId = email.Id }, "emails");
            }
            else
            {
                await SendEmailAsync(email.Id);
            }
        }

        public async Task SendEmailAsync(int emailId, CancellationToken cancellationToken = default)
        {
            var email = await _emailRepository.GetByIdAsync(emailId, cancellationToken);

            if (email == null)
            {
                throw new InvalidOperationException($"Email {emailId} was not found.");
            }

            email.Status = EmailStatus.Processing;
            await _emailRepository.SaveChangesAsync(cancellationToken);

            try
            {
                await _resendService.EmailSendAsync(new EmailMessage
                {
                    From = "onboarding@resend.dev",
                    To = "zackzack93@hotmail.com",
                    Subject = email.Subject,
                    HtmlBody = email.Body
                }, cancellationToken);

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

            await _emailRepository.SaveChangesAsync(cancellationToken);
        }

        public Task<EmailResponse?> GetEmailByIdAsync(int emailId)
        {
            return _emailRepository.GetResponseByIdAsync(emailId);
        }

        public Task<List<EmailResponse>> GetEmailsByUserIdAsync(int userId)
        {
            return _emailRepository.GetResponsesByUserIdAsync(userId);
        }

        public async Task<PaginatedResponse<EmailResponse>> GetEmailsAsync(int pageNumber, int pageSize)
        {
            var (pageNumberValid, pageSizeValid) = NormalizePagination(pageNumber, pageSize);

            var (items, totalCount) = await _emailRepository.GetPaginatedAsync(pageNumberValid, pageSizeValid);
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

        public async Task<PaginatedResponse<EmailResponse>> SearchEmailsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var (pageNumberValid, pageSizeValid) = NormalizePagination(pageNumber, pageSize);

            var (items, totalCount) = await _emailRepository.SearchPaginatedAsync(searchTerm, pageNumberValid, pageSizeValid);
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