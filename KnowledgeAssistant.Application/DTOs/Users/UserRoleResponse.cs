using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Domain.Entities.Roles;
using KnowledgeAssistant.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Users
{
    public class UserRoleResponse
    {
        public int RoleId { get; set; }
        public RoleResponse Role { get; set; } = null!;
    }
}
