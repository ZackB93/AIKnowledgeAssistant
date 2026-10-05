using System.Reflection;
using KnowledgeAssistant.API.Controllers;
using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Emails;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KnowledgeAssistant.Tests.API.Controller;

public class EmailControllerTests
{
    private readonly Mock<IEmailService> _emails = new();

    private EmailController Create() => new EmailController(_emails.Object).WithUser();

    [Fact]
    public async Task GetEmail_Missing_ReturnsNotFound()
    {
        _emails.Setup(s => s.GetEmailByIdAsync(1)).ReturnsAsync((EmailResponse?)null);

        var result = await Create().GetEmail(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEmail_Exists_ReturnsOk()
    {
        var email = new EmailResponse { Id = 1 };
        _emails.Setup(s => s.GetEmailByIdAsync(1)).ReturnsAsync(email);

        var result = await Create().GetEmail(1);

        Assert.Same(email, result.BodyOf().Data);
    }

    [Fact]
    public async Task GetEmailsByUserId_NoEmails_ReturnsNotFound()
    {
        _emails.Setup(s => s.GetEmailsByUserIdAsync(9)).ReturnsAsync(new List<EmailResponse>());

        var result = await Create().GetEmailsByUserId(9);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEmails_PageSizeOmitted_DefaultsToTen()
    {
        _emails.Setup(s => s.GetEmailsAsync(1, 10)).ReturnsAsync(new PaginatedResponse<EmailResponse>());

        await Create().GetEmails(1);

        _emails.Verify(s => s.GetEmailsAsync(1, 10), Times.Once);
    }
}