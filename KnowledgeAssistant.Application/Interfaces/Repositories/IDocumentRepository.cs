using KnowledgeAssistant.Application.DTOs.Documents;
using KnowledgeAssistant.Domain.Entities.Documents;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IDocumentRepository
    {
        Task<bool> DoesUserOwnChatSessionAsync(int chatSessionId, int userId, CancellationToken ct = default);
        Task AddDocumentAsync(Document document, CancellationToken ct = default);
        Task<List<DocumentChunkSearchProjection>> GetDocumentChunksForSearchAsync(List<int> documentIds, int userId, CancellationToken ct = default);
        Task<(List<DocumentResponse> Items, int TotalCount)> GetPaginatedDocumentsAsync(int pageNumber, int pageSize, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }

    public record DocumentChunkSearchProjection(
        long Id,
        int DocumentId,
        int ChunkIndex,
        string Content,
        byte[]? Embedding,
        string FileName
    );
}