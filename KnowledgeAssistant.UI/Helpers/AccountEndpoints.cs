using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;

namespace KnowledgeAssistant.UI.Helpers
{
    public static class AccountEndpoints
    {
        public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/Account/EstablishSession", async (HttpContext httpContext, string code, string? returnUrl, IMemoryCache cache) =>
            {
                if (!cache.TryGetValue<SignInResponse>(code, out var session) || session is null)
                {
                    return Results.Redirect("/signin?error=Session+expired.+Please+try+again.");
                }

                cache.Remove(code);

                var TokenHandler = new JwtSecurityTokenHandler();
                var JwtToken = TokenHandler.ReadJwtToken(session.Token);
                var TokenExpires = JwtToken.ValidTo;

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, session.User.Id.ToString()),
                    new(ClaimTypes.Name, $"{session.User.FirstName} {session.User.LastName}"),
                    new(ClaimTypes.Email, session.User.Email),
                    new("access_token", session.Token!),
                    new("access_token_expires", new DateTimeOffset(TokenExpires, TimeSpan.Zero).ToUnixTimeSeconds().ToString()),
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties { IsPersistent = true, ExpiresUtc = TokenExpires });

                var redirectUrl = string.IsNullOrEmpty(returnUrl) ? "/dashboard" : returnUrl;
                return Results.Redirect(redirectUrl);
            });

            app.MapGet("/Account/Logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/signin");
            });
        }
    }
}