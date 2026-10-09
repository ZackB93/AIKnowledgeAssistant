using KnowledgeAssistant.Application.DTOs.Documents;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Documents;
using KnowledgeAssistant.Infrastructure.Data; // Replace with your actual DbContext namespace
using KnowledgeAssistant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly KnowledgeContext _context;

        public DocumentRepository(KnowledgeContext context)
        {
            _context = context;
        }

        public async Task<bool> DoesUserOwnChatSessionAsync(int chatSessionId, int userId, CancellationToken ct)
        {
            return await _context.ChatSessions
                .AnyAsync(x => x.Id == chatSessionId && x.UserId == userId, ct);
        }

        public async Task AddDocumentAsync(Document document, CancellationToken ct)
        {
            await _context.Documents.AddAsync(document, ct);
        }

        public async Task<List<DocumentChunkSearchProjection>> GetDocumentChunksForSearchAsync(
            List<int> documentIds,
            int userId,
            CancellationToken ct)
        {
            return await _context.DocumentChunks
                .AsNoTracking()
                .Where(x =>
                    documentIds.Contains(x.DocumentId) &&
                    x.Document.UserId == userId)
                .Select(x => new DocumentChunkSearchProjection(
                    x.Id,
                    x.DocumentId,
                    x.ChunkIndex,
                    x.Content,
                    x.Embedding,
                    x.Document.FileName
                ))
                .ToListAsync(ct);
        }

        public async Task<(List<DocumentResponse> Items, int TotalCount)> GetPaginatedDocumentsAsync(
            int pageNumber,
            int pageSize,
            CancellationToken ct)
        {
            var totalCount = await _context.Documents.CountAsync(ct);

            var items = await _context.Documents
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new DocumentResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserName = x.User.FirstName + " " + x.User.LastName,
                    ChatSessionId = x.ChatSessionId,
                    ChatSessionTitle = x.ChatSession != null ? x.ChatSession.Title : null,
                    FileName = x.FileName,
                    ContentType = x.ContentType,
                    SizeBytes = x.SizeBytes,
                    Status = x.Status,
                    ErrorMessage = x.ErrorMessage,
                    ChunkCount = x.Chunks.Count,
                    CreatedAt = x.CreatedAt
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