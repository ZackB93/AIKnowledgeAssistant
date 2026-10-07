using KnowledgeAssistant.Application.DTOs.Roles;

namespace KnowledgeAssistant.Application.DTOs.Authentication
{
    public class SignInDetails
    {
        public int UserId { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool IsDeleted { get; set; }
        public List<RoleResponse> Roles { get; set; } = new();
    }
}