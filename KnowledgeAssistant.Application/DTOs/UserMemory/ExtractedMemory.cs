using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.UserMemory
{
    public class ExtractedMemory
    {
        public string MemoryType { get; set; } = string.Empty;

        public string NormalizedKey { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
