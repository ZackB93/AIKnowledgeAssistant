using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.Documents;
using KnowledgeAssistant.Application.Entities.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System.Runtime.InteropServices;
using DocChunkEntity = KnowledgeAssistant.Application.Entities.Documents.DocumentChunk;
using DocEntity = KnowledgeAssistant.Application.Entities.Documents.Document;
using OpenXml = DocumentFormat.OpenXml;

namespace KnowledgeAssistant.Application.Services
{
    public interface IDocumentService
    {
        Task<DocumentResponse> UploadDocumentAsync(IFormFile file, int? chatSessionId, int userId, CancellationToken cancellationToken = default);
    }

    public class DocumentService : IDocumentService
    {
        private const long MaxFileBytes = 10 * 1024 * 1024;
        private const int EmbeddingBatchSize = 64;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".txt", ".md" };
        private readonly KnowledgeContext _context;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;

        public DocumentService(KnowledgeContext context, IEmbeddingGenerator<string, Embedding<float>> embeddings)
        {
            _context = context;
            _embeddings = embeddings;
        }

        public async Task<DocumentResponse> UploadDocumentAsync(IFormFile file, int? chatSessionId, int userId, CancellationToken cancellationToken = default)
        {
            // Validate the file.
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

            // If the document is scoped to a chat, make sure the user owns that chat.
            if (chatSessionId is not null)
            {
                var ownsSession = await _context.ChatSessions
                    .AnyAsync(x => x.Id == chatSessionId && x.UserId == userId, cancellationToken);

                if (!ownsSession)
                {
                    throw new InvalidOperationException("Chat session was not found.");
                }
            }

            // Extract, chunk, embed.
            var text = await ExtractTextAsync(file, extension, cancellationToken);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("No text could be extracted from this file.");
            }

            var chunks = Chunk(text);
            var embeddings = new List<Embedding<float>>(chunks.Count);

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
                        var paragraphs = doc.MainDocumentPart?.Document.Body?
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

        private static List<string> Chunk(string text, int size = 800, int overlap = 150)
        {
            text = text.Trim();
            var chunks = new List<string>();
            var start = 0;

            while (start < text.Length)
            {
                var length = Math.Min(size, text.Length - start);

                // Try to end the chunk on a paragraph or sentence boundary rather than mid-word.
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
