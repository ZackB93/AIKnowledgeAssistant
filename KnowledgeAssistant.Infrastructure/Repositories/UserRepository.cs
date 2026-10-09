using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Users;
using KnowledgeAssistant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly KnowledgeContext _context;

        public UserRepository(KnowledgeContext context)
        {
            _context = context;
        }

        public async Task AddAsync(User user, CancellationToken ct)
        {
            await _context.Users.AddAsync(user, ct);
        }

        public async Task<User?> GetByIdWithCredentialsAndRolesAsync(int id, CancellationToken ct)
        {
            return await _context.Users
                .Include(x => x.Credentials)
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<SignInDetails?> GetSignInDetailsByEmailAsync(string email, CancellationToken ct)
        {
            return await _context.UserCredentials
                .AsNoTracking()
                .Where(x => x.EmailAddress == email)
                .Select(x => new SignInDetails
                {
                    UserId = x.UserId,
                    EmailAddress = x.EmailAddress,
                    PasswordHash = x.PasswordHash,
                    FirstName = x.User.FirstName,
                    Enabled = x.User.Enabled,
                    IsDeleted = x.User.IsDeleted,
                    Roles = x.User.UserRoles.Select(ur => new RoleResponse
                    {
                        Id = ur.Role.Id,
                        Name = ur.Role.Name,
                        Description = ur.Role.Description
                    }).ToList()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<UserResponse?> GetResponseByIdAsync(int id, CancellationToken ct)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Credentials.EmailAddress,
                    AddressLine1 = x.AddressLine1,
                    AddressLine2 = x.AddressLine2,
                    AddressLine3 = x.AddressLine3,
                    Postcode = x.Postcode,
                    Location = x.Location,
                    Enabled = x.Enabled,
                    IsDeleted = x.IsDeleted,
                    CreatedDateTime = x.CreatedDateTime,
                    Roles = x.UserRoles.Select(ur => new UserRoleResponse
                    {
                        RoleId = ur.Role.Id,
                        Role = new RoleResponse
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            Description = ur.Role.Description
                        }
                    }).ToList()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<(List<UserResponse> Items, int TotalCount)> GetPagedResponsesAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            var totalCount = await _context.Users.CountAsync(ct);

            var items = await _context.Users
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    CreatedDateTime = x.CreatedDateTime,
                    Email = x.Credentials.EmailAddress
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<(List<UserResponse> Items, int TotalCount)> SearchPagedResponsesAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x =>
                    x.FirstName.Contains(searchTerm) ||
                    x.LastName.Contains(searchTerm) ||
                    x.Credentials.EmailAddress.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Credentials.EmailAddress,
                    CreatedDateTime = x.CreatedDateTime
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)
        {
            return await _context.UserCredentials
                .AsNoTracking()
                .AnyAsync(x => x.EmailAddress == email, ct);
        }

        public async Task UpdatePasswordHashAsync(int userId, string newHash, CancellationToken ct)
        {
            await _context.UserCredentials
                .Where(x => x.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PasswordHash, newHash), ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}