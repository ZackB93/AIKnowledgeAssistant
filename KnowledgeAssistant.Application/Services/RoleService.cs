using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Entities.Roles;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Application.Services
{
    public interface IRoleService
    {
        Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request);
        Task<RoleResponse?> UpdateRoleAsync(UpdateRoleRequest request);
        Task<List<RoleResponse>> GetRolesAsync();
        Task<RoleResponse?> GetRoleByIdAsync(int id);
    }

    public class RoleService : IRoleService
    {
        private readonly KnowledgeContext _context;
        private readonly ICacheService _cacheService;
        private const string rolesCacheKey = "roles";
        private static string roleCacheKey(int id) => $"role_{id}";

        public RoleService(KnowledgeContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }
 
        public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request)
        {
            var role = new Role
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim()
            };

            _context.Roles.Add(role);

            await _context.SaveChangesAsync();

            _cacheService.RemoveFromCache(rolesCacheKey);

            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedDateTime = role.CreatedDateTime
            };
        }

        public async Task<RoleResponse?> UpdateRoleAsync(UpdateRoleRequest request)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (role is null)
            {
                return null;
            }

            role.Name = request.Name.Trim();
            role.Description = request.Description?.Trim();

            await _context.SaveChangesAsync();

            _cacheService.RemoveFromCache(rolesCacheKey);
            _cacheService.RemoveFromCache(roleCacheKey(role.Id));

            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedDateTime = role.CreatedDateTime
            };
        }

        public async Task<List<RoleResponse>> GetRolesAsync()
        {
            var cachedRoles = _cacheService.GetFromCache<List<RoleResponse>>(rolesCacheKey);
            if (cachedRoles is not null) return cachedRoles;

            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new RoleResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    CreatedDateTime = x.CreatedDateTime
                })
                .ToListAsync();

            _cacheService.SaveToCache(rolesCacheKey, roles, TimeSpan.FromHours(1));

            return roles;
        }

        public async Task<RoleResponse?> GetRoleByIdAsync(int id)
        {
            var cachedRole = _cacheService.GetFromCache<RoleResponse>(roleCacheKey(id));
            if (cachedRole is not null) return cachedRole;

            var role = await _context.Roles
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new RoleResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    CreatedDateTime = x.CreatedDateTime
                })
                .FirstOrDefaultAsync();

            _cacheService.SaveToCache(roleCacheKey(id), role, TimeSpan.FromHours(1));

            return role;
        }
    }
}
