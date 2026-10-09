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
    }
}