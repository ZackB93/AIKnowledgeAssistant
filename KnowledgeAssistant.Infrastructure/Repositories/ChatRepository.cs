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
                    Messages = new()
                })
                .ToListAsync(ct);
        }

        public async Task AddChatMessageAsync(ChatMessage message, CancellationToken ct)
        {
            await _context.ChatMessages.AddAsync(message, ct);
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
    }
}