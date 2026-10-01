using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Application.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

using ChatEntity = KnowledgeAssistant.Application.Entities.Chat.ChatMessage;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace KnowledgeAssistant.Application.Services;

public interface IChatService
{
    Task<ChatSessionResponse> AddChatSessionAsync(AddChatSessionRequest chatSessionRequest, int userId);
    Task<ChatMessageResponse> AddChatMessageAsync(AddChatMessageRequest chatMessageRequest, int userId);
    Task<List<ChatSessionResponse>> GetChatSessionsByUserIdAsync(int userId, bool includeMessages = true);
}

public class ChatService : IChatService
{
    private readonly KnowledgeContext _context;
    private readonly IChatClient _chatClient;

    public ChatService(KnowledgeContext context, IChatClient chatClient)
    {
        _context = context;
        _chatClient = chatClient;
    }

    public async Task<ChatSessionResponse> AddChatSessionAsync(AddChatSessionRequest chatSessionRequest, int userId)
    {
        var now = DateTime.UtcNow;

        var chatSession = new ChatSession
        {
            UserId = userId,
            Title = chatSessionRequest.Title,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.ChatSessions.Add(chatSession);

        await _context.SaveChangesAsync();

        return new ChatSessionResponse
        {
            Id = chatSession.Id,
            UserId = userId,
            Title = chatSession.Title,
            CreatedAt = chatSession.CreatedAt,
            UpdatedAt = chatSession.UpdatedAt,
            Messages = []
        };
    }

    public async Task<ChatMessageResponse> AddChatMessageAsync(AddChatMessageRequest chatMessageRequest, int userId)
    {
        var chatSession = await _context.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == chatMessageRequest.ChatSessionId && x.UserId == userId);

        if (chatSession == null)
        {
            throw new InvalidOperationException("Chat session was not found.");
        }

        var now = DateTime.UtcNow;

        // Save the user's message.
        var userMessage = new ChatEntity
        {
            ChatSessionId = chatSession.Id,
            Role = "user",
            Content = chatMessageRequest.Content,
            CreatedAt = now
        };

        _context.ChatMessages.Add(userMessage);

        chatSession.UpdatedAt = now;

        await _context.SaveChangesAsync();

        // Load the conversation history.
        var chatMessages = await _context.ChatMessages
            .AsNoTracking()
            .Where(x => x.ChatSessionId == chatSession.Id)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();

        // Convert database messages into Microsoft.Extensions.AI messages.
        var messages = chatMessages
            .Select(x => new AIChatMessage
            {
                Role = x.Role switch
                {
                    "user" => ChatRole.User,
                    "assistant" => ChatRole.Assistant,
                    "system" => ChatRole.System,
                    _ => ChatRole.User
                },
                Contents = [new TextContent(x.Content)]
            })
            .ToList();

        // Send the conversation to the AI.
        var response = await _chatClient.GetResponseAsync(messages);

        var assistantContent = response.Text;

        // Save the AI response.
        var assistantMessage = new ChatEntity
        {
            ChatSessionId = chatSession.Id,
            Role = "assistant",
            Content = assistantContent,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(assistantMessage);

        chatSession.UpdatedAt = assistantMessage.CreatedAt;

        await _context.SaveChangesAsync();

        return new ChatMessageResponse
        {
            Id = assistantMessage.Id,
            ChatSessionId = assistantMessage.ChatSessionId,
            Role = assistantMessage.Role,
            Content = assistantMessage.Content,
            CreatedAt = assistantMessage.CreatedAt
        };
    }

    public async Task<List<ChatSessionResponse>> GetChatSessionsByUserIdAsync(int userId, bool includeMessages = true)
    {
        IQueryable<ChatSession> query = _context.ChatSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt);

        if (includeMessages)
        {
            query = query.Include(x => x.Messages);
        }

        var sessions = await query.ToListAsync();

        return sessions.Select(x => new ChatSessionResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                Title = x.Title,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Messages = includeMessages ? x.Messages
                    .OrderBy(m => m.CreatedAt)
                    .ThenBy(m => m.Id)
                    .Select(m => new ChatMessageResponse
                    {
                        Id = m.Id,
                        ChatSessionId = m.ChatSessionId,
                        Role = m.Role,
                        Content = m.Content,
                         CreatedAt = m.CreatedAt
                    }).ToList()
                : []
            }).ToList();
    }
}