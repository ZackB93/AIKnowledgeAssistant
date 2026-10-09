using CallRatingService.API.Middleware;
using KnowledgeAssistant.Application.Handlers;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Application.Services.Communication;
using KnowledgeAssistant.Application.Services.Identity;
using KnowledgeAssistant.Application.Services.Infrastructure;
using KnowledgeAssistant.Application.Services.Knowledge;
using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Domain.Entities.Notifications;
using KnowledgeAssistant.Domain.Entities.Users;
using KnowledgeAssistant.Infrastructure.BackgroundServices;
using KnowledgeAssistant.Infrastructure.Data.Context;
using KnowledgeAssistant.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Resend;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddDbContext<KnowledgeContext>(options => 
    options.UseSqlServer( builder.Configuration.GetConnectionString("KnowledgeDatabase")));

builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
        .WriteTo.MSSqlServer(
            connectionString: context.Configuration.GetConnectionString("KnowledgeDatabase"),
            sinkOptions: new MSSqlServerSinkOptions
            { 
                TableName = "ErrorLogs", AutoCreateSqlTable = true
            }).Enrich.FromLogContext();
});

builder.Services.AddRateLimiter(options =>
{
    // General API rate limit
    options.AddFixedWindowLimiter("globallimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    // Sign-in limit - separate bucket per IP
    options.AddPolicy("signinlimit", httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            ipAddress,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                error = "Too many requests.",
                message = "Login attempts exceeded, please try again later."
            },
            ct);
    };
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        return Task.CompletedTask;
    });
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddOutputCache();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

builder.Services.AddScoped<IMessageHandler<SendEmailMessage>, SendEmailHandler>();
builder.Services.AddScoped<IMessageHandler<InsertRecipientsChunkMessage>, InsertRecipientsChunkHandler>();

builder.Services.AddSingleton<IResend>(ResendClient.Create(builder.Configuration["Resend:Key"]!));
builder.Services.AddHostedService<RabbitMQBackgroundService>();

builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddChatClient(services => new ChatClient(
    builder.Configuration["OpenAI:Model"]!,
    builder.Configuration["OpenAI:ApiKey"]!
).AsIChatClient());

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(services => new EmbeddingClient(
    builder.Configuration["OpenAI:EmbeddingModel"]!,
    builder.Configuration["OpenAI:ApiKey"]!
).AsIEmbeddingGenerator());

var app = builder.Build();

app.UseOutputCache();
app.UseRateLimiter();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
