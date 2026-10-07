using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Roles;

namespace KnowledgeAssistant.Application.Services
{
    public interface IRoleService
    {
        Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
        Task<RoleResponse?> UpdateRoleAsync(UpdateRoleRequest request, CancellationToken cancellationToken = default);
        Task<List<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken = default);
        Task<RoleResponse?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);
    }

    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ICacheService _cacheService;

        private const string RolesCacheKey = "roles";
        private static string GetRoleCacheKey(int id) => $"role_{id}";

        public RoleService(IRoleRepository roleRepository, ICacheService cacheService)
        {
            _roleRepository = roleRepository;
            _cacheService = cacheService;
        }

        public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var role = new Role
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim()
            };

            await _roleRepository.AddAsync(role, cancellationToken);
            await _roleRepository.SaveChangesAsync(cancellationToken);

            _cacheService.RemoveFromCache(RolesCacheKey);

            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedDateTime = role.CreatedDateTime
            };
        }

        public async Task<RoleResponse?> UpdateRoleAsync(UpdateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);

            if (role is null)
            {
                return null;
            }

            role.Name = request.Name.Trim();
            role.Description = request.Description?.Trim();

            await _roleRepository.SaveChangesAsync(cancellationToken);

            _cacheService.RemoveFromCache(RolesCacheKey);
            _cacheService.RemoveFromCache(GetRoleCacheKey(role.Id));

            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedDateTime = role.CreatedDateTime
            };
        }

        public async Task<List<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var cachedRoles = _cacheService.GetFromCache<List<RoleResponse>>(RolesCacheKey);
            if (cachedRoles is not null) return cachedRoles;

            var roles = await _roleRepository.GetAllResponsesAsync(cancellationToken);

            _cacheService.SaveToCache(RolesCacheKey, roles, TimeSpan.FromHours(1));

            return roles;
        }

        public async Task<RoleResponse?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cacheKey = GetRoleCacheKey(id);
            var cachedRole = _cacheService.GetFromCache<RoleResponse>(cacheKey);
            if (cachedRole is not null) return cachedRole;

            var role = await _roleRepository.GetResponseByIdAsync(id, cancellationToken);

            if (role is not null)
            {
                _cacheService.SaveToCache(cacheKey, role, TimeSpan.FromHours(1));
            }

            return role;
        }
    }
}