using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace KnowledgeAssistant.Application.Services.Identity
{
    public interface ICurrentUserService
    {
        int UserId { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int UserId
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out var id) || id <= 0)
                {
                    throw new UnauthorizedAccessException("An authenticated user is required.");
                }

                return id;
            }
        }
    }
}
