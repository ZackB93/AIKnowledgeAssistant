using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Application.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using ChatEntity = KnowledgeAssistant.Application.Entities.Chat.ChatMessage;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace KnowledgeAssistant.Application.Services
{
    public interface IChatService
    {
        Task<ChatSessionResponse> AddChatSessionAsync(AddChatSessionRequest chatSessionRequest, int userId);
        Task<ChatMessageResponse> AddChatMessageAsync(AddChatMessageRequest chatMessageRequest, int userId);
        Task<List<ChatSessionResponse>> GetChatSessionsByUserIdAsync(int userId);
        Task<List<ChatMessageResponse>> GetChatMessagesBySessionIdAsync(int sessionId);
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
                Messages = new()
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
            var userMessage = new Application.Entities.Chat.ChatMessage
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

            // This is where the AI generates a response based on the conversation history.
            // The response can take a few seconds due to the AI processing time and model we are using.
            // You can adjust the model in the appsettings.json file.
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

            var userMessageCount = chatMessages.Count(x => x.Role == "user");
            var newTitleGenerated = false;

            // Generate a title if this is still a new chat.
            if (chatSession.Title == "New Chat" && userMessageCount >= 2)
            {
                var title = await GenerateChatTitleAsync(messages);
                chatSession.Title = title;
                newTitleGenerated = true;
            }

            await _context.SaveChangesAsync();

            return new ChatMessageResponse
            {
                Id = assistantMessage.Id,
                ChatSessionId = assistantMessage.ChatSessionId,
                Role = assistantMessage.Role,
                Content = assistantMessage.Content,
                NewTitle = newTitleGenerated ? chatSession.Title : null,
                CreatedAt = assistantMessage.CreatedAt
            };
        }

        public async Task<List<ChatSessionResponse>> GetChatSessionsByUserIdAsync(int userId)
        {
            return _context.ChatSessions
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new ChatSessionResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Title = x.Title,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                }).ToList();
        }

        public async Task<List<ChatMessageResponse>> GetChatMessagesBySessionIdAsync(int sessionId)
        {
            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(x => x.ChatSessionId == sessionId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return messages.Select(x => new ChatMessageResponse
            {
                Id = x.Id,
                ChatSessionId = x.ChatSessionId,
                Role = x.Role,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            }).ToList();
        }

        private async Task<string> GenerateChatTitleAsync(List<AIChatMessage> messages)
        {
            var titlePrompt = new AIChatMessage(ChatRole.System,
            """
                Generate a short title for this conversation.

                Rules:
                - Maximum 6 words
                - Describe the main topic of the conversation
                - Do not use quotation marks
                - Do not include prefixes such as "Title:"
                - Do not use markdown
                - Return only the title
            """);

            var titleMessages = new List<AIChatMessage>
            {
                titlePrompt
            };

            titleMessages.AddRange(messages);

            var response = await _chatClient.GetResponseAsync(titleMessages);

            return response.Text.Trim();
        }
    }
}