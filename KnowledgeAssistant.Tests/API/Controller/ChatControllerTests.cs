using KnowledgeAssistant.API.Controllers;
using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KnowledgeAssistant.Tests.API.Controller;

public class ChatControllerTests
{
    private readonly Mock<IChatService> _chat = new();

    private ChatController Create(string? userId = "42") => new ChatController(_chat.Object).WithUser(userId);

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public async Task GetChatSessionsByUserId_InvalidUserClaim_ReturnsUnauthorized(string? userId)
    {
        var result = await Create(userId).GetChatSessionsByUserIdAsync();

        Assert.IsType<UnauthorizedResult>(result.Result);
        _chat.Verify(s => s.GetChatSessionsByUserIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetChatSessionsByUserId_NoSessions_ReturnsNotFound()
    {
        _chat.Setup(s => s.GetChatSessionsByUserIdAsync(42)).ReturnsAsync(new List<ChatSessionResponse>());

        var result = await Create().GetChatSessionsByUserIdAsync();

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.False(result.BodyOf().IsSuccessful);
    }

    [Fact]
    public async Task GetChatSessionsByUserId_HasSessions_ReturnsOkForClaimUser()
    {
        var sessions = new List<ChatSessionResponse> { new() { Id = 1, UserId = 42, Title = "First" } };
        _chat.Setup(s => s.GetChatSessionsByUserIdAsync(42)).ReturnsAsync(sessions);

        var result = await Create().GetChatSessionsByUserIdAsync();

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(sessions, result.BodyOf().Data);
    }

    [Fact]
    public async Task GetChatMessagesBySessionId_NoMessages_ReturnsNotFound()
    {
        _chat.Setup(s => s.GetChatMessagesBySessionIdAsync(7)).ReturnsAsync(new List<ChatMessageResponse>());

        var result = await Create().GetChatMessagesBySessionIdAsync(7);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddChatSession_InvalidUserClaim_ReturnsUnauthorized()
    {
        var result = await Create(null).AddChatSession(new AddChatSessionRequest());

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task AddChatSession_Valid_PassesClaimUserIdToService()
    {
        var request = new AddChatSessionRequest { Title = "My chat" };
        var added = new ChatSessionResponse { Id = 5, UserId = 42 };
        _chat.Setup(s => s.AddChatSessionAsync(request, 42)).ReturnsAsync(added);

        var result = await Create().AddChatSession(request);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(added, result.BodyOf().Data);
    }

    [Fact]
    public async Task AddChatMessage_InvalidUserClaim_ReturnsUnauthorized()
    {
        var result = await Create("abc").AddChatMessage(new AddChatMessageRequest { Content = "hi" });

        Assert.IsType<UnauthorizedResult>(result.Result);
        _chat.Verify(s => s.AddChatMessageAsync(It.IsAny<AddChatMessageRequest>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddChatMessage_Valid_ReturnsOkWithAssistantReply()
    {
        var request = new AddChatMessageRequest { ChatSessionId = 3, Content = "What is RAG?" };
        var reply = new ChatMessageResponse { Id = 10, Role = "assistant" };
        _chat.Setup(s => s.AddChatMessageAsync(request, 42)).ReturnsAsync(reply);

        var result = await Create().AddChatMessage(request);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(reply, result.BodyOf().Data);
    }
}