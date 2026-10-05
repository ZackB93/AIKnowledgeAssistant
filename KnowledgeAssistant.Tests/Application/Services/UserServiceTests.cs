using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Entities.Users;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KnowledgeAssistant.Tests.Application.Services;

public class UserServiceTests : IDisposable
{
    private readonly KnowledgeContext _context;
    private readonly Mock<IPasswordHasher<User>> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IEmailService> _emails = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<KnowledgeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new KnowledgeContext(options);

        _service = new UserService(_context, _hasher.Object, _tokens.Object, _emails.Object, _cache.Object);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedUserAsync(string email = "jane@test.com", bool enabled = true, bool isDeleted = false)
    {
        var user = new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            AddressLine1 = "1 High St",
            Postcode = "SO14",
            Location = "Southampton",
            Enabled = enabled,
            IsDeleted = isDeleted
        };
        _context.UserCredentials.Add(new UserCredential { EmailAddress = email, PasswordHash = "hash", User = user });
        await _context.SaveChangesAsync();
    }

    private void PasswordVerifiesAs(PasswordVerificationResult result) =>
        _hasher.Setup(h => h.VerifyHashedPassword(It.IsAny<User>(), "hash", It.IsAny<string>())).Returns(result);

    // ---------- SignInAsync ----------

    [Fact]
    public async Task SignIn_UnknownEmail_Fails()
    {
        var result = await _service.SignInAsync(new SignIn { EmailAddress = "nobody@test.com", Password = "pw" });

        Assert.False(result.Success);
        Assert.Null(result.Token);
    }

    [Theory]
    [InlineData(false, false)] // disabled
    [InlineData(true, true)]   // deleted
    public async Task SignIn_DisabledOrDeletedUser_FailsWithoutCheckingPassword(bool enabled, bool deleted)
    {
        await SeedUserAsync(enabled: enabled, isDeleted: deleted);

        var result = await _service.SignInAsync(new SignIn { EmailAddress = "jane@test.com", Password = "pw" });

        Assert.False(result.Success);
        _hasher.Verify(h => h.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SignIn_WrongPassword_FailsAndIssuesNoToken()
    {
        await SeedUserAsync();
        PasswordVerifiesAs(PasswordVerificationResult.Failed);

        var result = await _service.SignInAsync(new SignIn { EmailAddress = "jane@test.com", Password = "bad" });

        Assert.False(result.Success);
        _tokens.Verify(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>?>()), Times.Never);
    }

    [Fact]
    public async Task SignIn_CorrectPassword_ReturnsTokenAndUser()
    {
        await SeedUserAsync();
        PasswordVerifiesAs(PasswordVerificationResult.Success);
        _tokens.Setup(t => t.GenerateToken(It.IsAny<string>(), "Jane", It.IsAny<IEnumerable<string>?>())).Returns("jwt");

        var result = await _service.SignInAsync(new SignIn { EmailAddress = "jane@test.com", Password = "pw" });

        Assert.True(result.Success);
        Assert.Equal("jwt", result.Token);
        Assert.Equal("jane@test.com", result.User!.Email);
    }

    // ---------- AddUserAsync ----------

    [Fact]
    public async Task AddUser_EmailAlreadyExists_ThrowsConflict()
    {
        await SeedUserAsync("jane@test.com");

        await Assert.ThrowsAsync<ConflictException>(() => _service.AddUserAsync(
            new CreateUserRequest { Email = " Jane@Test.com ", Password = "pw" }));
    }

    [Fact]
    public async Task AddUser_NewUser_SavesNormalisedEmailAndQueuesWelcomeEmail()
    {
        _hasher.Setup(h => h.HashPassword(It.IsAny<User>(), "Passw0rd!")).Returns("hashed");

        var response = await _service.AddUserAsync(new CreateUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            AddressLine1 = "1 High St",
            Postcode = "SO14",
            Location = "Southampton",
            Email = " New@Test.com ",
            Password = "Passw0rd!",
            ReenterPassword = "Passw0rd!"
        });

        Assert.Equal("new@test.com", response.Email);

        var stored = await _context.UserCredentials.SingleAsync();
        Assert.Equal("new@test.com", stored.EmailAddress);
        Assert.Equal("hashed", stored.PasswordHash);

        _emails.Verify(e => e.QueueEmailAsync(response.Id, "new@test.com", "Welcome", It.IsAny<string>(), true), Times.Once);
    }
}