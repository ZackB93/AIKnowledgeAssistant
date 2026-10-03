using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using KnowledgeAssistant.Application.Entities.Users;

namespace KnowledgeAssistant.Application.Entities.Roles
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = [];
    }
}
