using System.Security.Claims;
using KnowledgeAssistant.Application.DTOs.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeAssistant.Tests.API.Controller;

internal static class ControllerTestHelper
{
    // userId = null simulates a token with no NameIdentifier claim.
    public static T WithUser<T>(this T controller, string? userId = "1") where T : ControllerBase
    {
        var claims = new List<Claim>();
        if (userId is not null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
        return controller;
    }

    public static ApiResult BodyOf(this ActionResult<ApiResult> result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        return Assert.IsType<ApiResult>(objectResult.Value);
    }
}