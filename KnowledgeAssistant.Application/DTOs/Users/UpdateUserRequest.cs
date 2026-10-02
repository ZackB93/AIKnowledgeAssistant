using KnowledgeAssistant.Application.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Users
{
    public class UpdateUserRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedDateTime { get; set; }

        [Required(ErrorMessage = "Address line 1 is required")]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [StringLength(200)]
        public string? AddressLine3 { get; set; }

        [Required(ErrorMessage = "Postcode is required")]
        [StringLength(20)]
        public string Postcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public bool Enabled { get; set; }

        public List<UserRole> Roles { get; set; } = new List<UserRole>();
    }
}
