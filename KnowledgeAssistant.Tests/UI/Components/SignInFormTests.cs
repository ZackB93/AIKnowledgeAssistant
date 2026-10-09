using Bunit;
using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Authentication;
using KnowledgeAssistant.Application.Services.Infrastructure;
using KnowledgeAssistant.UI.Components.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;

namespace KnowledgeAssistant.Tests.UI.Components;

public class SignInFormTests : BunitContext
{
    private readonly Mock<IHttpService> _http = new();

    public SignInFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose; // MudBlazor makes JS calls we don't care about
        Services.AddMudServices();
        Services.AddSingleton(_http.Object);
        Services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
    }

    private IRenderedComponent<SignInForm> RenderAndSubmit(string email = "jane@test.com", string password = "pw")
    {
        var cut = Render<SignInForm>();
        var inputs = cut.FindAll("input");
        inputs[0].Change(email);
        inputs[1].Change(password);
        cut.Find("form").Submit();
        return cut;
    }

    [Fact]
    public void Render_ErrorParameter_ShowsErrorMessage()
    {
        var cut = Render<SignInForm>(p => p.Add(x => x.error, "Session has timed out"));

        Assert.Contains("Session has timed out", cut.Markup);
    }

    [Fact]
    public void Submit_EmptyForm_DoesNotCallApi()
    {
        var cut = Render<SignInForm>();

        cut.Find("form").Submit();

        _http.Verify(h => h.PostDataAsync(It.IsAny<string>(), It.IsAny<SignIn>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Submit_ApiRejectsCredentials_ShowsApiMessage()
    {
        _http.Setup(h => h.PostDataAsync("/User/SignIn", It.IsAny<SignIn>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ApiResult { IsSuccessful = false, Message = "Invalid username or password." });

        var cut = RenderAndSubmit();

        cut.WaitForAssertion(() => Assert.Contains("Invalid username or password.", cut.Markup));
    }

    [Fact]
    public void Submit_ApiAccepts_NavigatesToEstablishSession()
    {
        var response = new SignInResponse { Success = true, Token = "jwt" };
        _http.Setup(h => h.PostDataAsync("/User/SignIn", It.IsAny<SignIn>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ApiResult { IsSuccessful = true, Data = response });

        RenderAndSubmit();

        var nav = Services.GetRequiredService<NavigationManager>();
        cut_WaitFor(() => Assert.Contains("/Account/EstablishSession?code=", nav.Uri));
    }

    private static void cut_WaitFor(Action assertion)
    {
        // Simple poll: the submit handler is async, so give it a moment to finish.
        for (var i = 0; i < 50; i++)
        {
            try { assertion(); return; } catch { Thread.Sleep(20); }
        }
        assertion();
    }
}