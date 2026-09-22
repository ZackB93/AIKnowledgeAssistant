using KnowledgeAssistant.Application.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Authentication
{
    public class SignInResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public UserResponse? User { get; set; }
    }
}
