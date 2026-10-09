using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Services.Identity;
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> CreateRole(CreateRoleRequest request, CancellationToken ct)
        {
            var role = await _roleService.CreateRoleAsync(request, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = role
            });
        }

        [HttpPost("Update")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> UpdateRole(UpdateRoleRequest request, CancellationToken ct)
        {
            var role = await _roleService.GetRoleByIdAsync(request.Id, ct);

            if (role is null)
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "Role does not exist."
                });
            }

            var updatedRole = await _roleService.UpdateRoleAsync(request, ct );

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = updatedRole
            });
        }

        [HttpGet("GetAll")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> GetRoles(CancellationToken ct)
        {
            var roles = await _roleService.GetRolesAsync(ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = roles
            });
        }

        [HttpGet("GetById/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> GetRoleById(int id, CancellationToken ct)
        {
            var role = await _roleService.GetRoleByIdAsync(id, ct);

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