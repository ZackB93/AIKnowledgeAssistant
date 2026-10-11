using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace KnowledgeAssistant.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService UserService )
        {
            _userService = UserService;
        }

        [HttpPost("SignIn")]
        [AllowAnonymous]
        [EnableRateLimiting("signinlimit")]
        public async Task<ActionResult<ApiResult>> SignIn(SignIn request, CancellationToken ct)
        {
            var SignInResponse = await _userService.SignInAsync(request, ct);

            if (!SignInResponse.Success)
            {
                return Unauthorized(new ApiResult()
                { 
                    IsSuccessful = false,
                    Message = "Invalid username or password."
                });
            }

            return Ok(new ApiResult()
            { 
                IsSuccessful = true,
                Data = SignInResponse
            });
        }

        [HttpGet("GetById/{Id}")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetUserDetails(int id, CancellationToken ct)
        {
            var user = await _userService.GetUserDetailsAsync(id, ct);

            if (user is null)
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "User does not exist."
                });
            }

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = user
            });
        }

        [HttpGet("GetAll")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> GetUsers(int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var users = await _userService.GetUsersAsync(pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = users
            });
        }

        [HttpGet("Search")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> Search(string searchTerm, int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var searchedUsers = await _userService.SearchUsersAsync(searchTerm, pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = searchedUsers
            });
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> Create(CreateUserRequest request, CancellationToken ct)
        {
            var addedUser = await _userService.AddUserAsync(request, ct);
            var result = new ApiResult
            {
                IsSuccessful = true,
                Data = addedUser,
                Message = "User created successfully."
            };

            return CreatedAtAction(nameof(GetUserDetails), new { id = addedUser.Id }, result);
        }

        [HttpPost("Update")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> Update(UpdateUserRequest request, CancellationToken ct)
        {
            var updatedUser = await _userService.UpdateUserAsync(request, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = updatedUser
            });
        }

        [HttpGet("Exists")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> Exists(string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required.");
            }
               
            var exists = await _userService.UserExistsAsync(email, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = exists
            });
        }

        [HttpGet("Dashboard")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> Dashboard(CancellationToken ct)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var dashboardData = await _userService.GetDashboardDataAsync(userId, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = dashboardData
            });
        }

        [HttpGet("AdminDashboard")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> AdminDashboard(CancellationToken ct)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var dashboardData = await _userService.GetAdminDashboardDataAsync(userId, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = dashboardData
            });
        }
    }
}
