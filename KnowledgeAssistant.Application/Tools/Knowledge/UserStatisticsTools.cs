using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Application.Services.Infrastructure;

namespace KnowledgeAssistant.Application.Tools.Knowledge
{
    public interface IUserStatisticsTools
    {
        Task<int> GetUserCount(CancellationToken ct);
        Task<int> GetEnabledUserCount(CancellationToken ct);
        Task<int> GetUserCountCreatedSince(DateTime since, CancellationToken ct);
    }

    public class UserStatisticsTools : IUserStatisticsTools
    {
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;

        public UserStatisticsTools(IUserRepository userRepository, ICacheService cacheService)
        {
            _userRepository = userRepository;
            _cacheService = cacheService;
        }

        public async Task<int> GetUserCount(CancellationToken ct)
        {
            var cacheKey = "GetUserCount";
            var data = _cacheService.GetFromCache<int?>(cacheKey);
            if (data is not null) return (int)data;

            var userCount = await _userRepository.GetUserCountAsync(ct);

            _cacheService.SaveToCache<int>(cacheKey, userCount, TimeSpan.FromMinutes(1));

            return userCount;
        }

        public async Task<int> GetEnabledUserCount(CancellationToken ct)
        {
            var cacheKey = "GetEnabledUserCount";
            var data = _cacheService.GetFromCache<int?>(cacheKey);
            if (data is not null) return (int)data;

            var userCount = await _userRepository.GetEnabledUserCountAsync(ct);

            _cacheService.SaveToCache<int>(cacheKey, userCount, TimeSpan.FromMinutes(1));

            return userCount;
        }

        public async Task<int> GetUserCountCreatedSince(DateTime since, CancellationToken ct)
        {
            var cacheKey = $"GetUserCountCreatedSince:{since.ToString()}";
            var data = _cacheService.GetFromCache<int?>(cacheKey);
            if (data is not null) return (int)data;

            var userCount = await _userRepository.GetUserCountCreatedSinceAsync(since, ct);

            _cacheService.SaveToCache<int>(cacheKey, userCount, TimeSpan.FromMinutes(1));

            return userCount;
        }
    }
}
