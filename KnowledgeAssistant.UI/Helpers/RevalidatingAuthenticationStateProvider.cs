using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Globalization;

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
                return Task.FromResult(false);

            var expiryClaim = user.FindFirst("access_token_expires")?.Value;

            if (!long.TryParse(expiryClaim, out var expirySeconds))
                return Task.FromResult(false);

            var expiry = DateTimeOffset.FromUnixTimeSeconds(expirySeconds);
            var now = DateTimeOffset.UtcNow;

            Console.WriteLine($"Expiry: {expiry:o} | Now: {now:o} | Remaining: {expiry - now}");

            return Task.FromResult(now < expiry);
        }
    }
}
