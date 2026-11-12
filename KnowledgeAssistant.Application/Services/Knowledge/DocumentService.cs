using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Documents;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using DocChunkEntity = KnowledgeAssistant.Domain.Entities.Documents.DocumentChunk;
using DocEntity = KnowledgeAssistant.Domain.Entities.Documents.Document;
using OpenXml = DocumentFormat.OpenXml;

namespace KnowledgeAssistant.Application.Services.Knowledge
{
    public interface IDocumentService
    {
        Task<DocumentResponse> UploadAsync(IFormFile file, int userId, CancellationToken ct, int? chatSessionId = null);
        Task<List<DocumentSearchResult>> SearchAsync(string query, List<int>? documentIds, int userId, CancellationToken ct);
        Task<PaginatedResponse<DocumentResponse>> GetDocumentsAsync(int pageNumber, int pageSize, CancellationToken ct);
    }

    public class DocumentService : IDocumentService
    {
        private const long MaxFileBytes = 10 * 1024 * 1024;
        private const int EmbeddingBatchSize = 64;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".docx", ".txt", ".md"
            };

        private readonly IDocumentRepository _documentRepository;
        private readonly IAISecurityService _aiSecurityService;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            IDocumentRepository documentRepository,
            IAISecurityService aiSecurityService,
            IEmbeddingGenerator<string, Embedding<float>> embeddings,
            ILogger<DocumentService> logger)
        {
            _documentRepository = documentRepository;
            _aiSecurityService = aiSecurityService;
            _embeddings = embeddings;
            _logger = logger;
        }

        public async Task<DocumentResponse> UploadAsync(IFormFile file, int userId, CancellationToken ct, int? chatSessionId = null)
        {
            await ValidateUploadAsync(file, chatSessionId, userId, ct);

            // Extract text from the uploaded document.
            var text = await ExtractTextAsync(file, Path.GetExtension(file.FileName), ct);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException( "No text could be extracted from this file.");
            }

            // Scan for potential prompt injections.
            var scanResult = _aiSecurityService.ScanContent(text, ct);

            if (scanResult.IsSuspicious)
            {
                _logger.LogWarning(
                    "Document upload rejected due to potential prompt injection. UserId: {UserId}, Findings: {Findings}",
                    userId,
                    string.Join(", ", scanResult.Findings));

                throw new InvalidOperationException("The document potentially contains unsafe instructions and was rejected.");
            }

            // Split extracted text into chunks.
            var chunks = Chunk(text);
            var embeddings = new List<Embedding<float>>(chunks.Count);

            // Generate embeddings in batches.
            for (var i = 0; i < chunks.Count; i += EmbeddingBatchSize)
            {
                var batch = chunks.Skip(i).Take(EmbeddingBatchSize).ToList();
                var generated = await _embeddings.GenerateAsync(batch, cancellationToken: ct);

                embeddings.AddRange(generated);
            }

            if (embeddings.Count != chunks.Count)
            {
                throw new InvalidOperationException(
                    "Embedding generation returned an unexpected number of results.");
            }

            // Create document and store native SQL vectors.
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
                Chunks = chunks.Select((content, index) =>
                    new DocChunkEntity
                    {
                        ChunkIndex = index,
                        Content = content,
                        Embedding = new SqlVector<float>(embeddings[index].Vector)
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

        public async Task<List<DocumentSearchResult>> SearchAsync(string query, List<int>? documentIds, int userId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new();
            }

            // Generate the embedding for the user's question.
            var queryEmbedding = await _embeddings.GenerateAsync(query, cancellationToken: ct);

            // Convert to SQL Server's native vector representation.
            var vector = new SqlVector<float>(queryEmbedding.Vector);

            // SQL Server performs the similarity comparison.
            var searchResult = await _documentRepository.SearchSimilarChunksAsync(vector, documentIds, userId, ct);

            return searchResult;
        }

        public async Task<PaginatedResponse<DocumentResponse>> GetDocumentsAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            const int maxPageSize = 50;

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            if (pageSize > maxPageSize)
            {
                pageSize = maxPageSize;
            }

            var (documents, totalCount) = await _documentRepository.GetPaginatedDocumentsAsync(pageNumber, pageSize,ct);
            var totalPages = (int)Math.Ceiling( (double)totalCount / pageSize);

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
                throw new InvalidOperationException(
                    "No file was provided.");
            }

            if (file.Length > MaxFileBytes)
            {
                throw new InvalidOperationException(
                    "File is too large (max 10 MB).");
            }

            var extension = Path.GetExtension(file.FileName);

            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    $"Unsupported file type: {extension}");
            }

            if (chatSessionId is not null)
            {
                var ownsSession =
                    await _documentRepository.DoesUserOwnChatSessionAsync(
                        chatSessionId.Value,
                        userId,
                        ct);

                if (!ownsSession)
                {
                    throw new InvalidOperationException(
                        "Chat session was not found.");
                }
            }
        }

        private static async Task<string> ExtractTextAsync(IFormFile file, string extension, CancellationToken ct)
        {
            var text = string.Empty;

            extension = extension.ToLowerInvariant();

            if (extension is ".txt" or ".md")
            {
                await using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                text = await reader.ReadToEndAsync(ct);

                return text.Replace("\0", "");
            }

            await using var memory = new MemoryStream();

            await file.CopyToAsync(memory, ct);
            memory.Position = 0;

            switch (extension)
            {
                case ".pdf":
                    using (var pdf = UglyToad.PdfPig.PdfDocument.Open(memory))
                    {
                        var pages = new List<string>();

                        foreach (var page in pdf.GetPages())
                        {
                            ct.ThrowIfCancellationRequested();
                            pages.Add(page.Text);
                        }

                        text = string.Join("\n\n", pages);
                    }
                    break;

                case ".docx":
                    using (var doc =
                        OpenXml.Packaging.WordprocessingDocument.Open(memory, false))
                    {
                        var body = doc.MainDocumentPart?.Document?.Body;

                        if (body is null)
                        {
                            text = string.Empty;
                            break;
                        }

                        var paragraphs = new List<string>();

                        foreach (var paragraph in
                            body.Descendants<OpenXml.Wordprocessing.Paragraph>())
                        {
                            ct.ThrowIfCancellationRequested();

                            var paragraphText = paragraph.InnerText;

                            if (!string.IsNullOrWhiteSpace(paragraphText))
                            {
                                paragraphs.Add(paragraphText);
                            }
                        }

                        text = string.Join("\n\n", paragraphs);
                    }
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported file type: {extension}");
            }

            return text.Replace("\0", "");
        }

        private static List<string> Chunk(
            string text,
            int size = 800,
            int overlap = 150)
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

                    if (breakAt < size / 2)
                        breakAt = window.LastIndexOf(". ");

                    if (breakAt < size / 2)
                        breakAt = window.LastIndexOf(' ');

                    if (breakAt >= size / 2)
                        length = breakAt + 1;
                }

                var chunk = text.Substring(start, length).Trim();

                if (chunk.Length > 0)
                {
                    chunks.Add(chunk);
                }

                if (start + length >= text.Length)
                    break;

                start += Math.Max(length - overlap, 1);
            }

            return chunks;
        }
    }
}
