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

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedDateTime { get; set; }

        [Required(ErrorMessage = "Address line 1 is required")]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string AddressLine2 { get; set; } = string.Empty;

        [StringLength(200)]
        public string AddressLine3 { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postcode is required")]
        [StringLength(20)]
        public string Postcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Re-entered password is required")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ReenterPassword { get; set; } = string.Empty;
    }
}
