using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Entities.Roles;
using KnowledgeAssistant.Application.Entities.Users;
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
