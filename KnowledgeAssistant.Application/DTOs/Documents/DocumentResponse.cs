using KnowledgeAssistant.Domain.Entities.Documents;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Documents
{
    public class DocumentResponse
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; } = null!;

        public int? ChatSessionId { get; set; }

        public string? ChatSessionTitle { get; set; }

        public string FileName { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long SizeBytes { get; set; }

        public DocumentStatus Status { get; set; }

        public string? ErrorMessage { get; set; }

        public int ChunkCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public static string FormatFileSize(long sizeBytes)
        {
            if (sizeBytes < 1024)
                return $"{sizeBytes} B";

            if (sizeBytes < 1024 * 1024)
                return $"{sizeBytes / 1024.0:0.#} KB";

            if (sizeBytes < 1024 * 1024 * 1024)
                return $"{sizeBytes / (1024.0 * 1024):0.#} MB";

            return $"{sizeBytes / (1024.0 * 1024 * 1024):0.#} GB";
        }
    }
}
