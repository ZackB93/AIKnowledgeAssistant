using KnowledgeAssistant.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Notifications
{
    public class CreateNotificationRequest
    {
        [Required, StringLength(200, MinimumLength = 1)]
        public string Title { get; init; } = string.Empty;

        [Required, StringLength(2000, MinimumLength = 1)]
        public string Body { get; init; } = string.Empty;

        public int CreatedByUserId { get; init; }

        [Required, MinLength(1, ErrorMessage = "Select at least one recipient.")]
        public List<int> UserIds { get; init; } = new();
    }
}
