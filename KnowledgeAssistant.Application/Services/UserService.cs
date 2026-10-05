
using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeAssistant.Application.Services
{
    public interface IUserService
    {
        Task<SignInResponse> SignInAsync(SignIn SignIn);
        Task<PaginatedResponse<UserResponse>> SearchUsersAsync(string SearchTerm, int PageNumber, int PageSize);
        Task<UserResponse> AddUserAsync(CreateUserRequest User);
        Task<UserResponse> UpdateUserAsync(UpdateUserRequest Request);
        Task<PaginatedResponse<UserResponse>> GetUsersAsync(int PageNumber, int PageSize);
        Task<UserResponse?> GetUserDetailsAsync(int UserId);
        Task<UserExistsResponse> UserExistsAsync(string Email);
    }

    public class UserService : IUserService
    {
        private readonly KnowledgeContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ICacheService _cacheService;
        private static string userCacheKey(int userId) => $"user_{userId}";

        public UserService(
            KnowledgeContext context,
            IPasswordHasher<User> passwordHasher,
            ITokenService tokenService,
            IEmailService emailService,
            ICacheService cacheService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _emailService = emailService;
            _cacheService = cacheService;
        }

        public async Task<SignInResponse> SignInAsync(SignIn request)
        {
            var credentials = await _context.UserCredentials
                .AsNoTracking()
                .Where(x => x.EmailAddress == request.EmailAddress)
                .Select(x => new
                {
                    x.UserId,
                    x.EmailAddress,
                    x.PasswordHash,
                    x.User,
                    Roles = x.User.UserRoles
                        .Select(ur => new RoleResponse
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            Description = ur.Role.Description
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (credentials is null)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "Invalid email address or password."
                };
            }

            if (!credentials.User.Enabled || credentials.User.IsDeleted)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "This account is unavailable."
                };
            }

            var passwordResult = _passwordHasher.VerifyHashedPassword(
                credentials.User,
                credentials.PasswordHash,
                request.Password
            );

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "Invalid email address or password."
                };
            }

            if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                var newHash = _passwordHasher.HashPassword(credentials.User, request.Password);

                await _context.UserCredentials
                    .Where(x => x.UserId == credentials.UserId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.PasswordHash, newHash));
            }

            var token = _tokenService.GenerateToken(
                userId: credentials.User.Id.ToString(),
                username: credentials.User.FirstName,
                roles: credentials.Roles.Select(r => r.Name).ToList()
            );

            return new SignInResponse
            {
                Success = true,
                Message = "Sign in successful.",
                Token = token,
                User = new UserResponse
                {
                    Id = credentials.User.Id,
                    FirstName = credentials.User.FirstName,
                    LastName = credentials.User.LastName,
                    Email = credentials.EmailAddress,
                    Roles = credentials.Roles
                        .Select(r => new UserRoleResponse { RoleId = r.Id, Role = r })
                        .ToList()
                }
            };
        }
      
        public async Task<PaginatedResponse<UserResponse>> GetUsersAsync(int PageNumber, int PageSize)
        {
            var MaxPageSize = 50;

            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 10;
            if (PageSize > MaxPageSize) PageSize = MaxPageSize;

            var TotalCount = await _context.Users.CountAsync();
            var TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            var Users = await _context.Users
                .AsNoTracking()
                .Include(x => x.UserRoles)
                .OrderBy(x => x.Id)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    CreatedDateTime = x.CreatedDateTime,
                    Email = x.Credentials.EmailAddress,
                })
                .ToListAsync();

            return new PaginatedResponse<UserResponse>
            {
                Items = Users,
                PageNumber = PageNumber,
                PageSize = PageSize,
                TotalCount = TotalCount,
                TotalPages = TotalPages
            };
        }

        public async Task<PaginatedResponse<UserResponse>> SearchUsersAsync(string SearchTerm, int PageNumber, int PageSize)
        {
            var MaxPageSize = 50;

            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 10;
            if (PageSize > MaxPageSize) PageSize = MaxPageSize;

            var Query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                Query = Query.Where(x =>
                    x.FirstName.Contains(SearchTerm) ||
                    x.LastName.Contains(SearchTerm) ||
                    x.Credentials.EmailAddress.Contains(SearchTerm));
            }

            var TotalCount = await Query.CountAsync();
            var TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            var Users = await Query
                .OrderBy(x => x.Id)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Include(x => x.Credentials)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Credentials.EmailAddress,
                }).ToListAsync();

            return new PaginatedResponse<UserResponse>
            {
                Items = Users,
                PageNumber = PageNumber,
                PageSize = PageSize,
                TotalCount = TotalCount,
                TotalPages = TotalPages
            };
        }

        public async Task<UserResponse?> GetUserDetailsAsync(int UserId)
        {
            var cachedUser = _cacheService.GetFromCache<UserResponse>(userCacheKey(UserId));
            if (cachedUser is not null) return cachedUser;

            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Credentials)
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
                    Roles = x.UserRoles
                    .Select(ur => new UserRoleResponse
                    {
                        RoleId = ur.Role.Id,
                        Role = new RoleResponse
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            Description = ur.Role.Description
                        }
                    })
                    .ToList()
                }).FirstOrDefaultAsync(x => x.Id == UserId);

            if(user is not null)
            {
                _cacheService.SaveToCache(userCacheKey(user.Id), user, TimeSpan.FromHours(1));
            }

            return user;
        }

        public async Task<UserResponse> AddUserAsync(CreateUserRequest User)
        {
            var ExistingUser = await UserExistsAsync(User.Email);
            if(ExistingUser.Exists)
            {
                throw new ConflictException($"User with Email {User.Email} already exists.");
            }

            var NewUser = new User
            {
                FirstName = User.FirstName,
                LastName = User.LastName,
                AddressLine1 = User.AddressLine1,
                AddressLine2 = User.AddressLine2,
                AddressLine3 = User.AddressLine3,
                Postcode = User.Postcode,
                Location = User.Location,
                Enabled = User.Enabled,
                IsDeleted = false,
                CreatedDateTime = DateTime.UtcNow,
            };

            NewUser.Credentials = new UserCredential
            {
                EmailAddress = User.Email.Trim().ToLowerInvariant(),
                PasswordHash = _passwordHasher.HashPassword(NewUser, User.Password)
            };

            NewUser.UserRoles = User.RoleIds.Select(roleId => new UserRole
            {
                RoleId = roleId,
                User = NewUser
            })
            .ToList();

            _context.Users.Add(NewUser);

            await _context.SaveChangesAsync();

            await _emailService.QueueEmailAsync(
                NewUser.Id,
                NewUser.Credentials.EmailAddress,
                "Welcome",
                "Thank you for registering with Knowledge Assistant, your account has now been created!"
            );

            return new UserResponse
            {
                Id = NewUser.Id,
                FirstName = NewUser.FirstName,
                LastName = NewUser.LastName,
                Email = NewUser.Credentials.EmailAddress,
                AddressLine1 = NewUser.AddressLine1,
                AddressLine2 = NewUser.AddressLine2,
                AddressLine3 = NewUser.AddressLine3,
                Postcode = NewUser.Postcode,
                Location = NewUser.Location,
                Enabled = NewUser.Enabled,
                IsDeleted = NewUser.IsDeleted,
                CreatedDateTime = NewUser.CreatedDateTime
            };
        }

        public async Task<UserResponse> UpdateUserAsync(UpdateUserRequest Request)
        {
            var User = await _context.Users
                .Include(x => x.Credentials)
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == Request.Id);

            if (User is null)
            {
                throw new KeyNotFoundException($"User with Id {Request.Id} was not found.");
            }

            User.FirstName = Request.FirstName;
            User.LastName = Request.LastName;
            User.AddressLine1 = Request.AddressLine1;
            User.AddressLine2 = Request.AddressLine2;
            User.AddressLine3 = Request.AddressLine3;
            User.Postcode = Request.Postcode;
            User.Location = Request.Location;
            User.Enabled = Request.Enabled;

            // Replace existing roles
            User.UserRoles.Clear();

            User.UserRoles = Request.RoleIds
                .Select(roleId => new UserRole
                {
                    UserId = User.Id,
                    RoleId = roleId
                })
                .ToList();

            await _context.SaveChangesAsync();

            _cacheService.RemoveFromCache(userCacheKey(User.Id));

            return new UserResponse
            {
                Id = User.Id,
                FirstName = User.FirstName,
                LastName = User.LastName,
                Email = User.Credentials.EmailAddress,
                AddressLine1 = User.AddressLine1,
                AddressLine2 = User.AddressLine2,
                AddressLine3 = User.AddressLine3,
                Postcode = User.Postcode,
                Location = User.Location,
                Enabled = User.Enabled,
                IsDeleted = User.IsDeleted,
                CreatedDateTime = User.CreatedDateTime,
                Roles = User.UserRoles
                .Select(ur => new UserRoleResponse
                {
                    RoleId = ur.RoleId,
                    Role = new RoleResponse
                    {
                        Id = ur.Role.Id,
                        Name = ur.Role.Name,
                        Description = ur.Role.Description
                    }
                })
                .ToList()
            };
        }
        
        public async Task<UserExistsResponse> UserExistsAsync(string Email)
        {
            var UserExists = await _context.UserCredentials
                .AsNoTracking()
                .AnyAsync(x => x.EmailAddress == Email.Trim().ToLowerInvariant());

            return new UserExistsResponse { Exists = UserExists };
        }
    }
}
