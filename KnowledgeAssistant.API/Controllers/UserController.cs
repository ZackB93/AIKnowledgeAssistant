using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Users;
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

        [HttpGet("GetById/{Id}")]
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

        [HttpGet("GetAll")]
        [OutputCache(Duration = 60)]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetUsers(int PageNumber, int? PageSize = 10)
        {
            var Users = await _userService.GetUsersAsync(PageNumber, (int)PageSize!);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = Users
            });
        }

        [HttpGet("Search")]
        [OutputCache(Duration = 60)]
        [Authorize]
        public async Task<ActionResult<ApiResult>> Search(string SearchTerm, int PageNumber, int? PageSize = 10)
        {
            var Users = await _userService.SearchUsersAsync(SearchTerm, PageNumber, (int)PageSize!);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = Users
            });
        }

        [HttpPost("Add")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> Add(CreateUserRequest Request)
        {
            var AddedUser = await _userService.AddUserAsync(Request);

            return CreatedAtAction(nameof(GetUserDetails), new { id = AddedUser.Id }, AddedUser);
        }

        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> Update(UpdateUserRequest Request)
        {
            var UpdatedUser = await _userService.UpdateUserAsync(Request);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = UpdatedUser
            });
        }

        [HttpGet("Exists")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResult>> Exists(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                return BadRequest("Email is required.");
            }
               
            var Exists = await _userService.UserExistsAsync(Email);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = Exists
            });
        }
    }
}
