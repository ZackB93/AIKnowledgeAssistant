using KnowledgeAssistant.API.Controllers;
using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KnowledgeAssistant.Tests.API.Controller;

public class UserControllerTests
{
    private readonly Mock<IUserService> _users = new();

    private UserController Create() => new UserController(_users.Object).WithUser();

    [Fact]
    public async Task SignIn_Unsuccessful_ReturnsUnauthorizedWithGenericMessage()
    {
        var request = new SignIn { EmailAddress = "a@b.com", Password = "wrong" };
        _users.Setup(s => s.SignInAsync(request))
              .ReturnsAsync(new SignInResponse { Success = false, Message = "This account is unavailable." });

        var result = await Create().SignIn(request);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var body = result.BodyOf();
        Assert.False(body.IsSuccessful);
        Assert.Equal("Invalid username or password.", body.Message);
    }

    [Fact]
    public async Task SignIn_Successful_ReturnsOkWithResponse()
    {
        var request = new SignIn { EmailAddress = "a@b.com", Password = "right" };
        var response = new SignInResponse { Success = true, Token = "jwt" };
        _users.Setup(s => s.SignInAsync(request)).ReturnsAsync(response);

        var result = await Create().SignIn(request);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, result.BodyOf().Data);
    }

    [Fact]
    public async Task GetUserDetails_UserMissing_ReturnsNotFound()
    {
        _users.Setup(s => s.GetUserDetailsAsync(8)).ReturnsAsync((UserResponse?)null);

        var result = await Create().GetUserDetails(8);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("User does not exist.", result.BodyOf().Message);
    }

    [Fact]
    public async Task GetUserDetails_UserExists_ReturnsOk()
    {
        var user = new UserResponse { Id = 8 };
        _users.Setup(s => s.GetUserDetailsAsync(8)).ReturnsAsync(user);

        var result = await Create().GetUserDetails(8);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(user, result.BodyOf().Data);
    }

    [Fact]
    public async Task GetUsers_PageSizeOmitted_DefaultsToTen()
    {
        _users.Setup(s => s.GetUsersAsync(1, 10)).ReturnsAsync(new PaginatedResponse<UserResponse>());

        await Create().GetUsers(1);

        _users.Verify(s => s.GetUsersAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task Create_Valid_ReturnsCreatedAtGetUserDetails()
    {
        var request = new CreateUserRequest { Email = "jane@test.com" };
        var added = new UserResponse { Id = 15 };
        _users.Setup(s => s.AddUserAsync(request)).ReturnsAsync(added);

        var result = await Create().Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(nameof(UserController.GetUserDetails), created.ActionName);
        Assert.Equal(15, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task Create_ServiceThrowsConflict_PropagatesToGlobalHandler()
    {
        var request = new CreateUserRequest { Email = "dupe@test.com" };
        _users.Setup(s => s.AddUserAsync(request)).ThrowsAsync(new ConflictException("exists"));

        await Assert.ThrowsAsync<ConflictException>(() => Create().Create(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Exists_BlankEmail_ReturnsBadRequest(string email)
    {
        var result = await Create().Exists(email);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _users.Verify(s => s.UserExistsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Exists_ValidEmail_ReturnsOk()
    {
        var response = new UserExistsResponse { Exists = true };
        _users.Setup(s => s.UserExistsAsync("jane@test.com")).ReturnsAsync(response);

        var result = await Create().Exists("jane@test.com");

        Assert.Same(response, result.BodyOf().Data);
    }
}