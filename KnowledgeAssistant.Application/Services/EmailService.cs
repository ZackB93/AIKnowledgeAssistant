using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Emails;
using KnowledgeAssistant.Domain.Entities.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Resend;
using EmailStatus = KnowledgeAssistant.Domain.Entities.Emails.EmailStatus;

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
                await _rabbitMQService.PublishAsync(new SendEmailMessage() {  EmailId = email.Id }, "emails");
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
                     To = "zackzack93@hotmail.com", //Resend only lets you use your own email for testing. Need to use a domain to use publicly.
                     Subject = email.Subject,
                     HtmlBody = email.Body
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
                    UserId = x.UserId,
                    UserName = $"{x.User.FirstName} {x.User.LastName}",
                    To = x.To,
                    Subject = x.Subject,
                    Body = x.Body,  
                    IsHtml = x.IsHtml,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    SentAt = x.SentAt,
                    FailedAt = x.FailedAt,
                    ErrorMessage = x.ErrorMessage,
                    RetryCount = x.RetryCount
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
                    UserName = $"{x.User.FirstName} {x.User.LastName}",
                    To = x.To,
                    Subject = x.Subject,
                    Body = x.Body,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    SentAt = x.SentAt,
                    FailedAt = x.FailedAt,
                    ErrorMessage = x.ErrorMessage,
                }).ToListAsync();

            return emails;
        }

        public async Task<PaginatedResponse<EmailResponse>> GetEmailsAsync(int PageNumber, int PageSize)
        {
            var MaxPageSize = 50;

            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 10;
            if (PageSize > MaxPageSize) PageSize = MaxPageSize;

            var TotalCount = await _context.Emails.CountAsync();
            var TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            var emails = await _context.Emails
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new EmailResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserName = $"{x.User.FirstName} {x.User.LastName}",
                    To = x.To,
                    Subject = x.Subject,
                    Body = x.Body,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    SentAt = x.SentAt,
                    FailedAt = x.FailedAt,
                    ErrorMessage = x.ErrorMessage
                })
                .ToListAsync();

            return new PaginatedResponse<EmailResponse>
            {
                Items = emails,
                PageNumber = PageNumber,
                PageSize = PageSize,
                TotalCount = TotalCount,
                TotalPages = TotalPages
            };
        }

        public async Task<PaginatedResponse<EmailResponse>> SearchEmailsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var maxPageSize = 50;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var Query = _context.Emails.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                Query = Query.Where(x =>
                    x.To.Contains(searchTerm) ||
                    x.User.FirstName.Contains(searchTerm) ||
                    x.User.LastName.Contains(searchTerm));
            }

            var totalCount = await Query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var emails = await Query
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EmailResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserName = $"{x.User.FirstName} {x.User.LastName}",
                    To = x.To,
                    Subject = x.Subject,
                    Body = x.Body,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    SentAt = x.SentAt,
                    FailedAt = x.FailedAt,
                }).ToListAsync();

            return new PaginatedResponse<EmailResponse>
            {
                Items = emails,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}
