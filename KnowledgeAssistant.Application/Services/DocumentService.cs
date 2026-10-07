using DocumentFormat.OpenXml.Drawing.Charts;
using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Documents;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Domain.Entities.Chat;
using KnowledgeAssistant.Domain.Entities.Documents;
using KnowledgeAssistant.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System.Runtime.InteropServices;
using DocChunkEntity = KnowledgeAssistant.Domain.Entities.Documents.DocumentChunk;
using DocEntity = KnowledgeAssistant.Domain.Entities.Documents.Document;
using OpenXml = DocumentFormat.OpenXml;

namespace KnowledgeAssistant.Application.Services
{
    public interface IDocumentService
    {
        Task<DocumentResponse> UploadAsync(IFormFile file, int userId, CancellationToken cancellationToken = default, int? chatSessionId = null);
        Task<List<DocumentSearchResult>> SearchAsync(string query, List<int> documentIds, int userId, CancellationToken cancellationToken = default);
        Task<PaginatedResponse<DocumentResponse>> GetDocumentsAsync(int pageNumber, int pageSize);
    }

    public class DocumentService : IDocumentService
    {
        private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB
        private const int EmbeddingBatchSize = 64;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".txt", ".md" };
        private readonly KnowledgeContext _context;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;

        public DocumentService(KnowledgeContext context, IEmbeddingGenerator<string, Embedding<float>> embeddings)
        {
            _context = context;
            _embeddings = embeddings;
        }

        public async Task<DocumentResponse> UploadAsync(IFormFile file, int userId, CancellationToken cancellationToken = default, int? chatSessionId = null)
        {
            await ValidateUploadAsync(file, chatSessionId, userId, cancellationToken);

            var extension = Path.GetExtension(file.FileName);
       
            // Extract, chunk, embed.
            var text = await ExtractTextAsync(file, extension, cancellationToken);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("No text could be extracted from this file.");
            }

            var chunks = Chunk(text);
            var embeddings = new List<Embedding<float>>(chunks.Count);

            // Generate embeddings in batches to avoid overwhelming the embedding service.
            for (var i = 0; i < chunks.Count; i += EmbeddingBatchSize)
            {
                var batch = chunks.Skip(i).Take(EmbeddingBatchSize).ToList();
                var generated = await _embeddings.GenerateAsync(batch, cancellationToken: cancellationToken);

                embeddings.AddRange(generated);
            }

            if (embeddings.Count != chunks.Count)
            {
                throw new InvalidOperationException("Embedding generation returned an unexpected number of results.");
            }

            // Save the document and its chunks in one SaveChanges.
            var document = new DocEntity
            {
                UserId = userId,
                ChatSessionId = chatSessionId,
                FileName = Path.GetFileName(file.FileName),
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                Status = DocumentStatus.Ready,
                EmbeddingModel = _embeddings.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId,
                CreatedAt = DateTime.UtcNow,
                Chunks = chunks.Select((content, index) => new DocChunkEntity
                {
                    ChunkIndex = index,
                    Content = content,
                    Embedding = MemoryMarshal.AsBytes(embeddings[index].Vector.Span).ToArray()
                }).ToList()
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);

            return new DocumentResponse
            {
                Id = document.Id,
                ChatSessionId = document.ChatSessionId,
                FileName = document.FileName,
                ContentType = document.ContentType,
                SizeBytes = document.SizeBytes,
                Status = document.Status,
                ChunkCount = chunks.Count,
                CreatedAt = document.CreatedAt
            };
        }

        public async Task<List<DocumentSearchResult>> SearchAsync(string query, List<int> documentIds, int userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || !documentIds.Any())
            {
                return new();
            }

            // Generate an embedding for the user's question.
            var queryEmbedding = await _embeddings.GenerateAsync(query, cancellationToken: cancellationToken);
            var queryVector = queryEmbedding.Vector.ToArray();

            // Only retrieve chunks belonging to the user's selected documents.
            var chunks = await _context.DocumentChunks
                .AsNoTracking()
                .Where(x =>
                    documentIds.Contains(x.DocumentId) &&
                    x.Document.UserId == userId)
                .Select(x => new
                {
                    x.Id,
                    x.DocumentId,
                    x.ChunkIndex,
                    x.Content,
                    x.Embedding,
                    FileName = x.Document.FileName
                })
                .ToListAsync(cancellationToken);

            var results = new List<DocumentSearchResult>();

            // Compare the query embedding to each chunk's embedding and calculate similarity.
            foreach (var chunk in chunks)
            {
                if (chunk.Embedding is null || chunk.Embedding.Length == 0)
                {
                    continue;
                }

                var storedVector = MemoryMarshal.Cast<byte, float>(chunk.Embedding.AsSpan()).ToArray();

                if (storedVector.Length != queryVector.Length)
                {
                    continue;
                }

                var similarity = CosineSimilarity(queryVector, storedVector);

                results.Add(new DocumentSearchResult
                {
                    DocumentId = chunk.DocumentId,
                    ChunkId = chunk.Id,
                    FileName = chunk.FileName,
                    ChunkIndex = chunk.ChunkIndex,
                    Content = chunk.Content,
                    Similarity = similarity
                });
            }

            // Take the top 5 most similar chunks and return them to the user.
            return results
                .OrderByDescending(x => x.Similarity)
                .Take(5)
                .ToList();

        }

        public async Task<PaginatedResponse<DocumentResponse>> GetDocumentsAsync(int pageNumber, int pageSize)
        {
            var maxPageSize = 50;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var totalCount = await _context.Documents.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var documents = await _context.Documents
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
                .ToListAsync();

            return new PaginatedResponse<DocumentResponse>
            {
                Items = documents,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        // Validates the uploaded file and checks if the user owns the chat session (if provided).
        private async Task ValidateUploadAsync(IFormFile file, int? chatSessionId, int userId, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                throw new InvalidOperationException("No file was provided.");
            }

            if (file.Length > MaxFileBytes)
            {
                throw new InvalidOperationException("File is too large (max 10 MB).");
            }

            var extension = Path.GetExtension(file.FileName);

            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"Unsupported file type: {extension}");
            }

            if (chatSessionId is not null)
            {
                var ownsSession = await _context.ChatSessions
                    .AnyAsync(x => x.Id == chatSessionId && x.UserId == userId, cancellationToken);

                if (!ownsSession)
                {
                    throw new InvalidOperationException("Chat session was not found.");
                }
            }
        }

        // Calculates the cosine similarity between two vectors.
        private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            double dot = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (var i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            if (magnitudeA == 0 || magnitudeB == 0)
            {
                return 0;
            }

            return dot / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

        // Extracts text from a file based on its extension.
        private static async Task<string> ExtractTextAsync(IFormFile file, string extension, CancellationToken cancellationToken)
        {
            await using var memory = new MemoryStream();
            await file.CopyToAsync(memory, cancellationToken);

            memory.Position = 0;

            string text;

            switch (extension.ToLowerInvariant())
            {
                case ".txt":
                case ".md":
                    using (var reader = new StreamReader(memory, leaveOpen: true))
                        text = await reader.ReadToEndAsync(cancellationToken);
                    break;

                case ".pdf": 
                    using (var pdf = UglyToad.PdfPig.PdfDocument.Open(memory))
                        text = string.Join("\n\n", pdf.GetPages().Select(p => p.Text));
                    break;

                case ".docx":
                    using (var doc = OpenXml.Packaging.WordprocessingDocument.Open(memory, false))
                    {
                        var paragraphs = doc.MainDocumentPart?.Document?.Body?
                            .Descendants<OpenXml.Wordprocessing.Paragraph>()
                            .Select(p => p.InnerText)
                            .Where(p => !string.IsNullOrWhiteSpace(p));

                        text = paragraphs is null ? "" : string.Join("\n\n", paragraphs);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported file type: {extension}");
            }

            // Null characters can break some database providers.
            return text.Replace("\0", "");
        }

        // Splits a large text into smaller chunks, trying to end each chunk on a paragraph or sentence boundary.
        private static List<string> Chunk(string text, int size = 800, int overlap = 150)
        {
            text = text.Trim();
            var chunks = new List<string>();
            var start = 0;

            while (start < text.Length)
            {
                var length = Math.Min(size, text.Length - start);

                if (start + length < text.Length)
                {
                    var window = text.AsSpan(start, length);
                    var breakAt = window.LastIndexOf("\n\n");
                    if (breakAt < size / 2) breakAt = window.LastIndexOf(". ");
                    if (breakAt < size / 2) breakAt = window.LastIndexOf(' ');
                    if (breakAt >= size / 2) length = breakAt + 1;
                }

                var chunk = text.Substring(start, length).Trim();
                if (chunk.Length > 0) chunks.Add(chunk);

                if (start + length >= text.Length) break;
                start += Math.Max(length - overlap, 1);
            }

            return chunks;
        }
    }
}
