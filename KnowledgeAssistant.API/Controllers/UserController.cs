using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace KnowledgeAssistant.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("globallimit")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService UserService )
        {
            _userService = UserService;
        }

        [HttpPost("SignIn")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResult>> SignIn(SignIn SignIn)
        {
            var SignInResponse = await _userService.SignInAsync(SignIn);

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

        [HttpGet("GetUserById/{Id}")]
        [OutputCache(Duration = 60)]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetUserDetails(int Id)
        {
            var User = await _userService.GetUserDetailsAsync(Id);

            if (User is null)
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
                Data = User
            });
        }

        [HttpGet("GetUsers")]
        [OutputCache(Duration = 60)]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetUsers()
        {
            var Users = await _userService.GetUsersAsync();

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = Users
            });
        }
    }
}
