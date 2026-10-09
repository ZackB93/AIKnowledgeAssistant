using Microsoft.Extensions.Caching.Memory;

namespace KnowledgeAssistant.Application.Services.Infrastructure
{
    public interface ICacheService
    {
        T? GetFromCache<T>(string Key);
        void SaveToCache<T>(string Key, T Value, TimeSpan Expiration);
        void RemoveFromCache(string Key);
    }

    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public T? GetFromCache<T>(string key)
        {
            return _cache.TryGetValue(key, out T? value) ? value : default;
        }

        public void SaveToCache<T>(string Key, T? Value, TimeSpan Expiration)
        {
            if (Value is null) return;

            _cache.Set(Key, Value, Expiration);
        }

        public void RemoveFromCache(string Key)
        {
            _cache.Remove(Key);

        }
    }
}
