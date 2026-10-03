using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeAssistant.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost("Create")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> CreateRole(CreateRoleRequest request)
        {
            var role = await _roleService.CreateRoleAsync(request);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = role
            });
        }

        [HttpPut("Update")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> UpdateRole(UpdateRoleRequest request)
        {
            var role = await _roleService.GetRoleByIdAsync(request.Id);

            if (role is null)
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "Role does not exist."
                });
            }

            var updatedRole = await _roleService.UpdateRoleAsync(request);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = updatedRole
            });
        }

        [HttpGet("GetAll")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetRoles()
        {
            var roles = await _roleService.GetRolesAsync();

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = roles
            });
        }

        [HttpGet("GetById/{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetRoleById(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);

            if (role is null)
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "Role does not exist."
                });
            }

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = role
            });
        }
    }
}