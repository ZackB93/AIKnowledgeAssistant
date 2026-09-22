using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Users
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string AddressLine1 { get; set; } = default!;
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string Postcode { get; set; } = default!;
        public string Location { get; set; } = default!;
        public bool Enabled { get; set; } = true;
        public DateTime CreatedDateTime { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
