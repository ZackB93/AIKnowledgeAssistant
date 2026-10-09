using KnowledgeAssistant.Application.DTOs.Emails;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class EmailRepository : IEmailRepository
    {
        private readonly KnowledgeContext _context;

        public EmailRepository(KnowledgeContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Email email, CancellationToken ct)
        {
            await _context.Emails.AddAsync(email, ct);
        }

        public async Task<Email?> GetByIdAsync(int emailId, CancellationToken ct = default)
        {
            return await _context.Emails
                .FirstOrDefaultAsync(x => x.Id == emailId, ct);
        }

        public async Task<EmailResponse?> GetResponseByIdAsync(int emailId, CancellationToken ct)
        {
            return await _context.Emails
                .AsNoTracking()
                .Where(x => x.Id == emailId)
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
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<EmailResponse>> GetResponsesByUserIdAsync(int userId, CancellationToken ct)
        {
            return await _context.Emails
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
                })
                .ToListAsync(ct);
        }

        public async Task<(List<EmailResponse> Items, int TotalCount)> GetPaginatedAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            var totalCount = await _context.Emails.CountAsync(ct);

            var items = await _context.Emails
                .AsNoTracking()
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
                    ErrorMessage = x.ErrorMessage
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<(List<EmailResponse> Items, int TotalCount)> SearchPaginatedAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _context.Emails.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    x.To.Contains(searchTerm) ||
                    x.User.FirstName.Contains(searchTerm) ||
                    x.User.LastName.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
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
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}