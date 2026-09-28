
using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
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

        public UserService(KnowledgeContext context, IPasswordHasher<User> passwordHasher, ITokenService tokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<SignInResponse> SignInAsync(SignIn SignIn)
        {
            // Find credentials and associated userI w
            var Credentials = await _context.UserCredentials
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.EmailAddress == SignIn.EmailAddress);

            // Don't reveal whether the email exists
            if (Credentials is null)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "Invalid email address or password."
                };
            }

            // Check whether account is available
            if (!Credentials.User.Enabled || Credentials.User.IsDeleted)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "This account is unavailable."
                };
            }

            // Verify password
            var PasswordResult = _passwordHasher.VerifyHashedPassword(
                Credentials.User,
                Credentials.PasswordHash,
                SignIn.Password);

            if (PasswordResult == PasswordVerificationResult.Failed)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "Invalid email address or password."
                };
            }

            if (PasswordResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                Credentials.PasswordHash = _passwordHasher.HashPassword(Credentials.User, SignIn.Password);
                _context.UserCredentials.Update(Credentials);
                await _context.SaveChangesAsync();
            }

            var Token = _tokenService.GenerateToken(userId: Credentials.User.Id.ToString(), username: Credentials.User.FirstName);
                
            return new SignInResponse
            {
                Success = true,
                Message = "Sign in successful.",
                Token = Token,
                User = new UserResponse
                {
                    Id = Credentials.User.Id,
                    FirstName = Credentials.User.FirstName,
                    LastName = Credentials.User.LastName,
                    Email = Credentials.EmailAddress
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
                .OrderBy(x => x.Id)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    CreatedDateTime = x.CreatedDateTime
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
            var User = await _context.Users
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
                    CreatedDateTime = x.CreatedDateTime
                }).FirstOrDefaultAsync(x => x.Id == UserId);

            return User;
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

            _context.Users.Add(NewUser);
            await _context.SaveChangesAsync();

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

            await _context.SaveChangesAsync();

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
                CreatedDateTime = User.CreatedDateTime
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
