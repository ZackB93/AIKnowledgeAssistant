using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        public async Task<ActionResult<ApiResult>> SignIn(SignIn request)
        {
            var SignInResponse = await _userService.SignInAsync(request);

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
        public async Task<ActionResult<ApiResult>> GetUserDetails(int id)
        {
            var user = await _userService.GetUserDetailsAsync(id);

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
        public async Task<ActionResult<ApiResult>> GetUsers(int pageNumber, int? pageSize = 10)
        {
            var users = await _userService.GetUsersAsync(pageNumber, (int)pageSize!);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = users
            });
        }

        [HttpGet("Search")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> Search(string searchTerm, int pageNumber, int? pageSize = 10)
        {
            var searchedUsers = await _userService.SearchUsersAsync(searchTerm, pageNumber, (int)pageSize!);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = searchedUsers
            });
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> Create(CreateUserRequest request)
        {
            var addedUser = await _userService.AddUserAsync(request);
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
        public async Task<ActionResult<ApiResult>> Update(UpdateUserRequest request)
        {
            var updatedUser = await _userService.UpdateUserAsync(request);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = updatedUser
            });
        }

        [HttpGet("Exists")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> Exists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required.");
            }
               
            var exists = await _userService.UserExistsAsync(email);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = exists
            });
        }
    }
}
