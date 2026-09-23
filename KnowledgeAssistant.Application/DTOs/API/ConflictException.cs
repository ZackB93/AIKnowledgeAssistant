using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.API
{
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
