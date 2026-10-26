using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Chat;
using KnowledgeAssistant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly KnowledgeContext _context;

        public ChatRepository(KnowledgeContext context)
        {
            _context = context;
        }

        public async Task AddChatSessionAsync(ChatSession session, CancellationToken ct)
        {
            await _context.ChatSessions.AddAsync(session, ct);
        }

        public async Task<ChatSession?> GetChatSessionByIdAndUserIdAsync(int sessionId, int userId, CancellationToken ct)
        {
            return await _context.ChatSessions
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct);
        }

        public async Task<List<ChatSessionResponse>> GetChatSessionsByUserIdAsync(int userId, CancellationToken ct)
        {
            return await _context.ChatSessions
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new ChatSessionResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Title = x.Title,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                })
                .ToListAsync(ct);
        }

        public async Task AddChatMessageAsync(ChatMessage message, CancellationToken ct)
        {
            await _context.ChatMessages.AddAsync(message, ct);
            await SaveChangesAsync(ct);
        }

        public async Task<List<ChatMessage>> GetChatMessagesBySessionIdAsync(int sessionId, CancellationToken ct)
        {
            return await _context.ChatMessages
                .AsNoTracking()
                .Where(x => x.ChatSessionId == sessionId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
        }

        public async Task AttachDocumentsToMessageAsync(int chatSessionId, long messageId, List<int> documentIds, int userId, CancellationToken ct)
        {
            if (!documentIds.Any())
            {
                return;
            }

            var documents = await _context.Documents
                .Where(x =>
                    documentIds.Contains(x.Id) &&
                    x.UserId == userId &&
                    x.ChatSessionId == chatSessionId)
                .ToListAsync(ct);

            foreach (var document in documents)
            {
                document.ChatMessageId = messageId;
            }
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task<List<ChatSessionResponse>> GetRecentChatSessionsByUserIdAsync(int userId, int count, CancellationToken ct)
        {
            count = Math.Clamp(count, 1, 20);

            return await _context.ChatSessions
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.UpdatedAt)
                .Take(count)
                .Select(x => new ChatSessionResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Title = x.Title,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                })
                .ToListAsync(ct);
        }

        public async Task<List<ChatMessage>> GetChatMessagesBySessionIdAndUserIdAsync(int sessionId, int userId, int limit, CancellationToken ct)
        {
            limit = Math.Clamp(limit, 1, 50);

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(x =>
                    x.ChatSessionId == sessionId &&
                    _context.ChatSessions.Any(s =>
                        s.Id == x.ChatSessionId &&
                        s.UserId == userId))
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(limit)
                .ToListAsync(ct);

            messages.Reverse();

            return messages;
        }

        public async Task<List<ChatHistorySearchResult>> SearchChatHistoryAsync(int userId, string query, int limit, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new();
            }

            limit = Math.Clamp(limit, 1, 20);

            return await (
                from message in _context.ChatMessages.AsNoTracking()
                join session in _context.ChatSessions.AsNoTracking()
                    on message.ChatSessionId equals session.Id
                where session.UserId == userId &&
                      message.Content.Contains(query)
                orderby message.CreatedAt descending
                select new ChatHistorySearchResult
                {
                    ChatSessionId = session.Id,
                    ChatTitle = session.Title,
                    Role = message.Role,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt
                })
                .Take(limit)
                .ToListAsync(ct);
        }
    }
}