using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Application.Services.Infrastructure;

namespace KnowledgeAssistant.Application.Tools.Knowledge
{
    public interface IUserStatisticsTools
    {
        Task<int> GetUserCount(CancellationToken ct);
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
    }
}
