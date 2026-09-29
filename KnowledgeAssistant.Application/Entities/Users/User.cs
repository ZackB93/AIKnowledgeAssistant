using KnowledgeAssistant.Application.Entities.Emails;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Entities.Users
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
        public ICollection<UserDocument> Documents { get; set; } = [];
        public ICollection<Email> Emails { get; set; } = [];
    }
}
