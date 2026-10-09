using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Roles;
using KnowledgeAssistant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly KnowledgeContext _context;

        public RoleRepository(KnowledgeContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Role role, CancellationToken ct)
        {
            await _context.Roles.AddAsync(role, ct);
        }

        public async Task<Role?> GetByIdAsync(int id, CancellationToken ct)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<RoleResponse?> GetResponseByIdAsync(int id, CancellationToken ct)
        {
            return await _context.Roles
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new RoleResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    CreatedDateTime = x.CreatedDateTime
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<RoleResponse>> GetAllResponsesAsync(CancellationToken ct)
        {
            return await _context.Roles
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new RoleResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    CreatedDateTime = x.CreatedDateTime
                })
                .ToListAsync(ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}