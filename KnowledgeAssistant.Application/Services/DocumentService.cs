using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Documents;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using System.Runtime.InteropServices;
using DocChunkEntity = KnowledgeAssistant.Domain.Entities.Documents.DocumentChunk;
using DocEntity = KnowledgeAssistant.Domain.Entities.Documents.Document;
using OpenXml = DocumentFormat.OpenXml;

namespace KnowledgeAssistant.Application.Services
{
    public interface IDocumentService
    {
        Task<DocumentResponse> UploadAsync(IFormFile file, int userId, CancellationToken ct, int? chatSessionId = null);
        Task<List<DocumentSearchResult>> SearchAsync(string query, List<int> documentIds, int userId, CancellationToken ct);
        Task<PaginatedResponse<DocumentResponse>> GetDocumentsAsync(int pageNumber, int pageSize, CancellationToken ct);
    }

    public class DocumentService : IDocumentService
    {
        private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB
        private const int EmbeddingBatchSize = 64;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".txt", ".md" };

        private readonly IDocumentRepository _documentRepository;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;

        public DocumentService(
            IDocumentRepository documentRepository,
            IEmbeddingGenerator<string, Embedding<float>> embeddings)
        {
            _documentRepository = documentRepository;
            _embeddings = embeddings;
        }

        public async Task<DocumentResponse> UploadAsync(IFormFile file, int userId, CancellationToken ct, int? chatSessionId = null)
        {
            await ValidateUploadAsync(file, chatSessionId, userId, ct);

            var extension = Path.GetExtension(file.FileName);

            // Extract, chunk, embed.
            var text = await ExtractTextAsync(file, extension, ct);

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
                var generated = await _embeddings.GenerateAsync(batch, cancellationToken: ct);

                embeddings.AddRange(generated);
            }

            if (embeddings.Count != chunks.Count)
            {
                throw new InvalidOperationException("Embedding generation returned an unexpected number of results.");
            }

            // Save the document and its chunks via repository.
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

            await _documentRepository.AddDocumentAsync(document, ct);
            await _documentRepository.SaveChangesAsync(ct);

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

        public async Task<List<DocumentSearchResult>> SearchAsync(string query, List<int> documentIds, int userId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query) || !documentIds.Any())
            {
                return new();
            }

            // Generate an embedding for the user's question.
            var queryEmbedding = await _embeddings.GenerateAsync(query, cancellationToken: ct);

            // Keep as ReadOnlyMemory<float> (or float[] array) across await boundaries.
            ReadOnlyMemory<float> queryMemory = queryEmbedding.Vector;

            // Only retrieve chunks belonging to the user's selected documents.
            var chunks = await _documentRepository.GetDocumentChunksForSearchAsync(documentIds, userId, ct);

            var results = new List<DocumentSearchResult>();

            // Extract the span here, safely after all async/await calls are finished.
            ReadOnlySpan<float> queryVector = queryMemory.Span;

            // Compare the query embedding to each chunk's embedding and calculate similarity.
            foreach (var chunk in chunks)
            {
                if (chunk.Embedding is null || chunk.Embedding.Length == 0)
                {
                    continue;
                }

                ReadOnlySpan<float> storedVector = MemoryMarshal.Cast<byte, float>(chunk.Embedding.AsSpan());

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

            // Take the top 5 most similar chunks.
            return results
                .OrderByDescending(x => x.Similarity)
                .Take(5)
                .ToList();
        }

        public async Task<PaginatedResponse<DocumentResponse>> GetDocumentsAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            var maxPageSize = 50;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var (documents, totalCount) = await _documentRepository.GetPaginatedDocumentsAsync(pageNumber, pageSize, ct);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PaginatedResponse<DocumentResponse>
            {
                Items = documents,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        private async Task ValidateUploadAsync(IFormFile file, int? chatSessionId, int userId, CancellationToken ct)
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
                var ownsSession = await _documentRepository.DoesUserOwnChatSessionAsync(chatSessionId.Value, userId, ct);

                if (!ownsSession)
                {
                    throw new InvalidOperationException("Chat session was not found.");
                }
            }
        }

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

        private static async Task<string> ExtractTextAsync(IFormFile file, string extension, CancellationToken ct)
        {
            await using var memory = new MemoryStream();
            await file.CopyToAsync(memory, ct);

            memory.Position = 0;

            string text;

            switch (extension.ToLowerInvariant())
            {
                case ".txt":
                case ".md":
                    using (var reader = new StreamReader(memory, leaveOpen: true))
                        text = await reader.ReadToEndAsync(ct);
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

            return text.Replace("\0", "");
        }

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