using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Entities.Roles;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Application.Services
{
    public interface IRoleService
    {
        Task<RoleResponse> CreateRoleAsync(CreateRoleRequest Request);
        Task<RoleResponse?> UpdateRoleAsync(UpdateRoleRequest Request);
        Task<List<RoleResponse>> GetRolesAsync();
        Task<RoleResponse?> GetRoleByIdAsync(int Id);
    }

    public class RoleService : IRoleService
    {
        private readonly KnowledgeContext _context;

        public RoleService(KnowledgeContext context)
        {
            _context = context;
        }
 
        public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest Request)
        {
            var role = new Role
            {
                Name = Request.Name.Trim(),
                Description = Request.Description.Trim()
            };

            _context.Roles.Add(role);

            await _context.SaveChangesAsync();

            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            };
        }

        public async Task<RoleResponse?> UpdateRoleAsync(UpdateRoleRequest Request)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(x => x.Id == Request.Id);

            if (role is null)
            {
                return null;
            }

            role.Name = Request.Name.Trim();
            role.Description = Request.Description?.Trim();

            await _context.SaveChangesAsync();

            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            };
        }

        public async Task<List<RoleResponse>> GetRolesAsync()
        {
            return await _context.Roles
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new RoleResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description
                })
                .ToListAsync();
        }

        public async Task<RoleResponse?> GetRoleByIdAsync(int Id)
        {
            return await _context.Roles
                .AsNoTracking()
                .Where(x => x.Id == Id)
                .Select(x => new RoleResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description
                })
                .FirstOrDefaultAsync();
        }
    }
}
