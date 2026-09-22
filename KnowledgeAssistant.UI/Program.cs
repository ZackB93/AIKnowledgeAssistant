using KnowledgeAssistant.Application.Services;
using KnowledgeAssistant.UI.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMemoryCache();
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();
builder.Services.AddLocalStorageServices();

// HTTP client
builder.Services.AddHttpClient("ExternalClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:ExternalClientUrl"]!);
});

builder.Services.AddScoped<IHttpService, HttpService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProviderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
