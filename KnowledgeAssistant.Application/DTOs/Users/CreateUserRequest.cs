using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace KnowledgeAssistant.Application.DTOs.Users
{
    public class CreateUserRequest
    {

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; } = default!;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        public string LastName { get; set; } = default!;

        [Required(ErrorMessage = "Address line 1 is required")]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = default!;
        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [StringLength(200)]
        public string? AddressLine3 { get; set; }

        [Required(ErrorMessage = "Postcode is required")]
        [StringLength(20)]
        public string Postcode { get; set; } = default!;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100)]
        public string Location { get; set; } = default!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = default!;

        [Required(ErrorMessage = "Rentered password is required")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ReenterPassword { get; set; } = default!;
    }
}
