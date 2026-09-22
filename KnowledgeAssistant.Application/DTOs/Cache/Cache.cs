using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Cache
{
    public class Cache
    {
        public string? CacheKey { get; set; } = null;
        public object? Data { get; set; } = null;
        public int CacheTimeMins { get; set; } = 1;
    }
}
