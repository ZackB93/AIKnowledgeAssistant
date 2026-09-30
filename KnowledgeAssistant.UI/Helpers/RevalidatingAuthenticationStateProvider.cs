using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace KnowledgeAssistant.UI.Helpers
{
    public class RevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
    {
        public RevalidatingAuthenticationStateProvider(ILoggerFactory loggerFactory) : base(loggerFactory)
        {

        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

        protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var user = authenticationState.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                return Task.FromResult(false);
            }

            var expiryClaim = user.FindFirst("access_token_expires")?.Value;

            if (!DateTime.TryParse(expiryClaim, out var expiry))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(DateTime.UtcNow < expiry);
        }
    }
}
