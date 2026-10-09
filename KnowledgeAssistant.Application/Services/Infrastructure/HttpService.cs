using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Cache;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KnowledgeAssistant.Application.Services.Infrastructure
{
    public interface IHttpService
    {
        Task<ApiResult> GetDataAsync(string Endpoint, Cache? CacheData = null, CancellationToken ct = default);
        Task<ApiResult> PostDataAsync<TRequest>(string Endpoint, TRequest Payload, CancellationToken ct = default);
        Task<ApiResult> PostFileAsync(string url, MultipartFormDataContent content, CancellationToken ct = default);
    }

    public class HttpService : IHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICacheService _cacheService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly NavigationManager _navigationManager;

        public HttpService(IHttpClientFactory httpClientFactory, ICacheService cacheService, IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager)
        {
            _httpClientFactory = httpClientFactory;
            _cacheService = cacheService;
            _httpContextAccessor = httpContextAccessor;
            _navigationManager = navigationManager;
        }

        public async Task<ApiResult> GetDataAsync(string Endpoint, Cache? CacheData = null, CancellationToken ct = default)
        {
            // Check if data is in cache first
            if (CacheData?.CacheKey is not null)
            {
                var ExistingCachedData = _cacheService.GetFromCache<object>(CacheData.CacheKey);
                if (ExistingCachedData is not null)
                {
                    return new ApiResult { IsSuccessful = true, Data = ExistingCachedData };
                }
            }

            try
            {
                var Client = _httpClientFactory.CreateClient("ExternalClient");
                var Token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;

                if (!string.IsNullOrEmpty(Token))
                {
                    if (TokenExpired())
                    {
                        _navigationManager.NavigateTo($"/signin?error=Session+expired&returnUrl={Uri.EscapeDataString(_navigationManager.Uri)}");
                    }

                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                }

                var Response = await Client.GetAsync(Endpoint, ct);
                var Result = await Response.Content.ReadFromJsonAsync<ApiResult>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken: ct);

                if (Result is null)
                {
                    return new ApiResult { IsSuccessful = false, Message = "The API returned an empty response." };
                }

                if (Result.IsSuccessful && Result.Data is not null && CacheData?.CacheKey is not null)
                {
                    _cacheService.SaveToCache(CacheData.CacheKey, Result.Data, TimeSpan.FromMinutes(CacheData.CacheTimeMins));
                }

                return Result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new ApiResult { IsSuccessful = false, Message = "Request cancelled." };
            }
            catch (HttpRequestException ex)
            {
                return new ApiResult { IsSuccessful = false, Message = ex.Message };
            }
            catch (JsonException ex)
            {
                return new ApiResult { IsSuccessful = false, Message = $"Deserialization failed: {ex.Message}" };
            }
        }

        public async Task<ApiResult> PostDataAsync<TRequest>(string Endpoint, TRequest Payload, CancellationToken ct = default)
        {
            try
            {
                var Client = _httpClientFactory.CreateClient("ExternalClient");
                var Token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;

                if (!string.IsNullOrEmpty(Token))
                {
                    if (TokenExpired())
                    {
                        _navigationManager.NavigateTo($"/signin?error=Session+expired&returnUrl={Uri.EscapeDataString(_navigationManager.Uri)}", forceLoad: true);
                    }

                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                }

                var Response = await Client.PostAsJsonAsync(Endpoint, Payload, ct);
                var Result = await Response.Content.ReadFromJsonAsync<ApiResult>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken: ct);

                return Result ?? new ApiResult { IsSuccessful = false, Message = "The API returned an empty response." };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new ApiResult { IsSuccessful = false, Message = "Request cancelled." };
            }
            catch (HttpRequestException ex)
            {
                return new ApiResult{ IsSuccessful = false, Message = ex.Message };
            }
            catch (JsonException ex)
            {
                return new ApiResult { IsSuccessful = false, Message = $"Deserialization failed: {ex.Message}" };
            }
        }

        public async Task<ApiResult> PostFileAsync(string Endpoint, MultipartFormDataContent Content, CancellationToken ct = default)
        {
            try
            {
                var Client = _httpClientFactory.CreateClient("ExternalClient");
                var Token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;

                if (!string.IsNullOrEmpty(Token))
                {
                    if (TokenExpired())
                    {
                        _navigationManager.NavigateTo($"/signin?error=Session+expired&returnUrl={Uri.EscapeDataString(_navigationManager.Uri)}", forceLoad: true);
                    }

                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                }

                var Response = await Client.PostAsync(Endpoint, Content, ct);

                var Result = await Response.Content.ReadFromJsonAsync<ApiResult>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken: ct);

                return Result ?? new ApiResult { IsSuccessful = false, Message = "The API returned an empty response." };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new ApiResult { IsSuccessful = false, Message = "Request cancelled." };
            }
            catch (HttpRequestException ex)
            {
                return new ApiResult { IsSuccessful = false, Message = ex.Message };
            }
            catch (JsonException ex)
            {
                return new ApiResult { IsSuccessful = false, Message = $"Deserialization failed: {ex.Message}" };
            }
        }

        private bool TokenExpired()
        {
            var expiry = _httpContextAccessor.HttpContext?
                .User
                .FindFirst("access_token_expires")?
                .Value;

            if (!long.TryParse(expiry, out var expiryUnix))
                return true;

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiryUnix;
        }

    }
}
