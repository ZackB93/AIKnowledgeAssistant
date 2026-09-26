using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Authentication
{
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
