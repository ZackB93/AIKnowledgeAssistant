using KnowledgeAssistant.Application.Data.Context;
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
        Task<List<UserResponse>> GetUsersAsync();
        Task<UserResponse?> GetUserDetailsAsync(int UserId);
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
            // Find credentials and associated user
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

        public async Task<List<UserResponse>> GetUsersAsync()
        {
            var Users = await _context.Users
                .AsNoTracking()
                .Include(x => x.Credentials)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Credentials.EmailAddress,
                }).ToListAsync();

            return Users;
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
    }
}
