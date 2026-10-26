using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Domain.Entities.Chat;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IChatRepository
    {
        Task AddChatSessionAsync(ChatSession session, CancellationToken ct);
        Task<ChatSession?> GetChatSessionByIdAndUserIdAsync(int sessionId, int userId, CancellationToken ct);
        Task<List<ChatSessionResponse>> GetChatSessionsByUserIdAsync(int userId, CancellationToken ct);
        Task AddChatMessageAsync(ChatMessage message, CancellationToken ct);
        Task<List<ChatMessage>> GetChatMessagesBySessionIdAsync(int sessionId, CancellationToken ct);
        Task AttachDocumentsToMessageAsync(int chatSessionId, long messageId, List<int> documentIds, int userId, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
        Task<List<ChatSessionResponse>> GetRecentChatSessionsByUserIdAsync(int userId, int count, CancellationToken ct);
        Task<List<ChatHistorySearchResult>> SearchChatHistoryAsync(int userId, string query, int limit, CancellationToken ct);
        Task<List<ChatMessage>> GetChatMessagesBySessionIdAndUserIdAsync(int sessionId, int userId, int limit, CancellationToken ct);
    }
}