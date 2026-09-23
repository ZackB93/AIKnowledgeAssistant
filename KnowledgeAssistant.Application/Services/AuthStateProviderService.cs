using KnowledgeAssistant.Application.DTOs.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace KnowledgeAssistant.Application.Services
{
    public class AuthStateProviderService : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly JsonWebTokenHandler _jwtHandler;
        private const string StorageKey = "UserSession";
        private readonly AuthenticationState _anonymousState;

        public AuthStateProviderService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            _jwtHandler = new JsonWebTokenHandler();
            _anonymousState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var session = await _localStorage.GetItemAsync<SignInResponse>(StorageKey);

                if (session == null || string.IsNullOrWhiteSpace(session.Token))
                {
                    return _anonymousState;
                }

                if (!IsTokenValidAndActive(session.Token, out var jwtToken))
                {
                    await SignOutAsync();
                    return _anonymousState;
                }

                var identity = new ClaimsIdentity(jwtToken.Claims,"jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                return _anonymousState;
            }
        }

        public async Task SignInAsync(SignInResponse session)
        {
            if (!IsTokenValidAndActive(session.Token,out var jwtToken))
            {
                await SignOutAsync();
                return;
            }

            await _localStorage.SetItemAsync(StorageKey, session);

            var identity = new ClaimsIdentity(jwtToken.Claims,"jwt");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public async Task SignOutAsync()
        {
            await _localStorage.RemoveItemAsync(StorageKey);

            NotifyAuthenticationStateChanged(Task.FromResult(_anonymousState));
        }

        private bool IsTokenValidAndActive(string? token,out JsonWebToken jwtToken)
        {
            jwtToken = null!;

            if (string.IsNullOrWhiteSpace(token) || !_jwtHandler.CanReadToken(token))
            {
                return false;
            }

            jwtToken = _jwtHandler.ReadJsonWebToken(token);

            return jwtToken.ValidTo > DateTime.UtcNow;
        }
    }

}
