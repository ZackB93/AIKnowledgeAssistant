using KnowledgeAssistant.API.Controllers;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KnowledgeAssistant.Tests.API.Controller;

public class RoleControllerTests
{
    private readonly Mock<IRoleService> _roles = new();

    private RoleController Create() => new RoleController(_roles.Object).WithUser();

    [Fact]
    public async Task UpdateRole_RoleMissing_ReturnsNotFoundAndDoesNotUpdate()
    {
        _roles.Setup(s => s.GetRoleByIdAsync(99, CancellationToken.None)).ReturnsAsync((RoleResponse?)null);

        var result = await Create().UpdateRole(new UpdateRoleRequest { Id = 99, Name = "Ghost" }, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        _roles.Verify(s => s.UpdateRoleAsync(It.IsAny<UpdateRoleRequest>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task UpdateRole_RoleExists_ReturnsOkWithUpdatedRole()
    {
        var request = new UpdateRoleRequest { Id = 3, Name = "Editor v2" };
        var updated = new RoleResponse { Id = 3, Name = "Editor v2" };
        _roles.Setup(s => s.GetRoleByIdAsync(3, CancellationToken.None)).ReturnsAsync(new RoleResponse { Id = 3 });
        _roles.Setup(s => s.UpdateRoleAsync(request, CancellationToken.None)).ReturnsAsync(updated);

        var result = await Create().UpdateRole(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, result.BodyOf().Data);
    }

    [Fact]
    public async Task GetRoleById_RoleMissing_ReturnsNotFound()
    {
        _roles.Setup(s => s.GetRoleByIdAsync(5, CancellationToken.None)).ReturnsAsync((RoleResponse?)null);

        var result = await Create().GetRoleById(5, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRoles_ReturnsOkWithRoles()
    {
        var roles = new List<RoleResponse> { new() { Id = 1, Name = "Admin" } };
        _roles.Setup(s => s.GetRolesAsync(CancellationToken.None)).ReturnsAsync(roles);

        var result = await Create().GetRoles(CancellationToken.None);

        Assert.Same(roles, result.BodyOf().Data);
    }
}