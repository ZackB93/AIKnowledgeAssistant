using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Domain.Entities.Users
{
    public class UserCredential
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string EmailAddress { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public User User { get; set; } = default!;
    }
}
