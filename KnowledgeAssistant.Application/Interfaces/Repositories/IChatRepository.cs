using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Domain.Entities.Chat;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IChatRepository
    {
        Task AddChatSessionAsync(ChatSession session, CancellationToken ct = default);
        Task<ChatSession?> GetChatSessionByIdAndUserIdAsync(int sessionId, int userId, CancellationToken ct = default);
        Task<List<ChatSessionResponse>> GetChatSessionsByUserIdAsync(int userId, CancellationToken ct = default);
        Task AddChatMessageAsync(ChatMessage message, CancellationToken ct = default);
        Task<List<ChatMessage>> GetChatMessagesBySessionIdAsync(int sessionId, CancellationToken ct = default);
        Task AttachDocumentsToMessageAsync(int chatSessionId, long messageId, List<int> documentIds, int userId, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}