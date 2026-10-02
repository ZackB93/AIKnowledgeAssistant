using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Roles
{
    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
