using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace KnowledgeAssistant.Application.Services
{
    public interface IUserService
    {
        Task<SignInResponse> SignInAsync(SignIn request, CancellationToken cancellationToken = default);
        Task<PaginatedResponse<UserResponse>> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<UserResponse> AddUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<UserResponse> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<PaginatedResponse<UserResponse>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetUserDetailsAsync(int userId, CancellationToken cancellationToken = default);
        Task<UserExistsResponse> UserExistsAsync(string email, CancellationToken cancellationToken = default);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ICacheService _cacheService;

        private static string GetUserCacheKey(int userId) => $"user_{userId}";

        public UserService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            ITokenService tokenService,
            IEmailService emailService,
            ICacheService cacheService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _emailService = emailService;
            _cacheService = cacheService;
        }

        public async Task<SignInResponse> SignInAsync(SignIn request, CancellationToken cancellationToken = default)
        {
            var credentials = await _userRepository.GetSignInDetailsByEmailAsync(request.EmailAddress, cancellationToken);

            if (credentials is null)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "Invalid email address or password."
                };
            }

            if (!credentials.Enabled || credentials.IsDeleted)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "This account is unavailable."
                };
            }

            var dummyUser = new User { Id = credentials.UserId };
            var passwordResult = _passwordHasher.VerifyHashedPassword(
                dummyUser,
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
                var newHash = _passwordHasher.HashPassword(dummyUser, request.Password);
                await _userRepository.UpdatePasswordHashAsync(credentials.UserId, newHash, cancellationToken);
            }

            var token = _tokenService.GenerateToken(
                userId: credentials.UserId.ToString(),
                username: credentials.FirstName,
                roles: credentials.Roles.Select(r => r.Name).ToList()
            );

            return new SignInResponse
            {
                Success = true,
                Message = "Sign in successful.",
                Token = token,
                User = new UserResponse
                {
                    Id = credentials.UserId,
                    FirstName = credentials.FirstName,
                    Email = credentials.EmailAddress,
                    Roles = credentials.Roles
                        .Select(r => new UserRoleResponse { RoleId = r.Id, Role = r })
                        .ToList()
                }
            };
        }

        public async Task<PaginatedResponse<UserResponse>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            var normalizedPageSize = Math.Clamp(pageSize < 1 ? 10 : pageSize, 1, 50);

            var (items, totalCount) = await _userRepository.GetPagedResponsesAsync(normalizedPageNumber, normalizedPageSize, cancellationToken);

            return new PaginatedResponse<UserResponse>
            {
                Items = items,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / normalizedPageSize)
            };
        }

        public async Task<PaginatedResponse<UserResponse>> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            var normalizedPageSize = Math.Clamp(pageSize < 1 ? 10 : pageSize, 1, 50);

            var (items, totalCount) = await _userRepository.SearchPagedResponsesAsync(searchTerm, normalizedPageNumber, normalizedPageSize, cancellationToken);

            return new PaginatedResponse<UserResponse>
            {
                Items = items,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / normalizedPageSize)
            };
        }

        public async Task<UserResponse?> GetUserDetailsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var cacheKey = GetUserCacheKey(userId);
            var cachedUser = _cacheService.GetFromCache<UserResponse>(cacheKey);
            if (cachedUser is not null) return cachedUser;

            var user = await _userRepository.GetResponseByIdAsync(userId, cancellationToken);

            if (user is not null)
            {
                _cacheService.SaveToCache(cacheKey, user, TimeSpan.FromHours(1));
            }

            return user;
        }

        public async Task<UserResponse> AddUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
            {
                throw new ConflictException($"User with Email {request.Email} already exists.");
            }

            var newUser = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                AddressLine3 = request.AddressLine3,
                Postcode = request.Postcode,
                Location = request.Location,
                Enabled = request.Enabled,
                IsDeleted = false,
                CreatedDateTime = DateTime.UtcNow
            };

            newUser.Credentials = new UserCredential
            {
                EmailAddress = normalizedEmail,
                PasswordHash = _passwordHasher.HashPassword(newUser, request.Password)
            };

            newUser.UserRoles = request.RoleIds.Select(roleId => new UserRole
            {
                RoleId = roleId,
                User = newUser
            }).ToList();

            await _userRepository.AddAsync(newUser, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            await _emailService.QueueEmailAsync(
                newUser.Id,
                newUser.Credentials.EmailAddress,
                "Welcome",
                "Thank you for registering with Knowledge Assistant, your account has now been created!"
            );

            return new UserResponse
            {
                Id = newUser.Id,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                Email = newUser.Credentials.EmailAddress,
                AddressLine1 = newUser.AddressLine1,
                AddressLine2 = newUser.AddressLine2,
                AddressLine3 = newUser.AddressLine3,
                Postcode = newUser.Postcode,
                Location = newUser.Location,
                Enabled = newUser.Enabled,
                IsDeleted = newUser.IsDeleted,
                CreatedDateTime = newUser.CreatedDateTime
            };
        }

        public async Task<UserResponse> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdWithCredentialsAndRolesAsync(request.Id, cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException($"User with Id {request.Id} was not found.");
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.AddressLine1 = request.AddressLine1;
            user.AddressLine2 = request.AddressLine2;
            user.AddressLine3 = request.AddressLine3;
            user.Postcode = request.Postcode;
            user.Location = request.Location;
            user.Enabled = request.Enabled;

            user.UserRoles.Clear();
            user.UserRoles = request.RoleIds.Select(roleId => new UserRole
            {
                UserId = user.Id,
                RoleId = roleId
            }).ToList();

            await _userRepository.SaveChangesAsync(cancellationToken);

            _cacheService.RemoveFromCache(GetUserCacheKey(user.Id));

            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Credentials.EmailAddress,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2,
                AddressLine3 = user.AddressLine3,
                Postcode = user.Postcode,
                Location = user.Location,
                Enabled = user.Enabled,
                IsDeleted = user.IsDeleted,
                CreatedDateTime = user.CreatedDateTime,
                Roles = user.UserRoles.Select(ur => new UserRoleResponse
                {
                    RoleId = ur.RoleId,
                    Role = ur.Role == null ? null! : new RoleResponse
                    {
                        Id = ur.Role.Id,
                        Name = ur.Role.Name,
                        Description = ur.Role.Description
                    }
                }).ToList()
            };
        }

        public async Task<UserExistsResponse> UserExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var exists = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);

            return new UserExistsResponse { Exists = exists };
        }
    }
}