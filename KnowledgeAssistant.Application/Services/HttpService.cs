using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Cache;
using System.Net.Http.Json;
using System.Text.Json;

namespace KnowledgeAssistant.Application.Services
{
    public interface IHttpService
    {
        Task<ApiResult> GetDataAsync(string Endpoint, Cache? CacheData = null, CancellationToken CancellationToken = default);
        Task<ApiResult> PostDataAsync<TRequest>(string Endpoint, TRequest Payload, CancellationToken CancellationToken = default);
    }

    public class HttpService : IHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICacheService _cacheService;

        public HttpService(IHttpClientFactory httpClientFactory, ICacheService cacheService)
        {
            _httpClientFactory = httpClientFactory;
            _cacheService = cacheService;
        }

        public async Task<ApiResult> GetDataAsync(string Endpoint, Cache? CacheData = null, CancellationToken CancellationToken = default)
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
                var Response = await Client.GetAsync(Endpoint, CancellationToken);

                var Result = await Response.Content.ReadFromJsonAsync<ApiResult>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken: CancellationToken);

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
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                throw;
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

        public async Task<ApiResult> PostDataAsync<TRequest>(string Endpoint, TRequest Payload, CancellationToken CancellationToken = default)
        {
            try
            {
                var Client = _httpClientFactory.CreateClient("ExternalClient");
                var Response = await Client.PostAsJsonAsync(Endpoint, Payload, CancellationToken);

                var Result = await Response.Content.ReadFromJsonAsync<ApiResult>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken: CancellationToken);

                return Result ?? new ApiResult { IsSuccessful = false, Message = "The API returned an empty response." };
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                throw;
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
    }
}
