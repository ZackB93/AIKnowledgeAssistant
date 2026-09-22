using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Entities.Users
{
    public class UserDocument
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long FileSize { get; set; }
        public string StoragePath { get; set; } = default!;
        public DateTime UploadedAt { get; set; }
        public User User { get; set; } = default!;
    }
}
