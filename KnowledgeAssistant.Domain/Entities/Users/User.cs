using KnowledgeAssistant.Domain.Entities.Chat;
using KnowledgeAssistant.Domain.Entities.Documents;
using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Domain.Entities.Notifications;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Domain.Entities.Users
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string AddressLine1 { get; set; } = default!;
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string Postcode { get; set; } = default!;
        public string Location { get; set; } = default!;
        public bool Enabled { get; set; } = true;
        public int CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public bool IsDeleted { get; set; } = false;
        public UserCredential Credentials { get; set; } = default!;
        public ICollection<Email> Emails { get; set; } = [];
        public ICollection<ChatSession> ChatSessions { get; set; } = [];
        public ICollection<UserRole> UserRoles { get; set; } = [];
        public ICollection<Document> Documents { get; set; } = [];
        public ICollection<NotificationRecipient> Notifications { get; set; } = [];

    }
}
