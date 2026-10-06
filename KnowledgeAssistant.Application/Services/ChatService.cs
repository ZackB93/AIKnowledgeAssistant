using KnowledgeAssistant.Application.Data.Context;
using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Application.DTOs.Documents;
using KnowledgeAssistant.Application.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatEntity = KnowledgeAssistant.Application.Entities.Chat.ChatMessage;

namespace KnowledgeAssistant.Application.Services
{
    public interface IChatService
    {
        Task<ChatSessionResponse> AddChatSessionAsync(AddChatSessionRequest chatSessionRequest, int userId);
        Task<ChatMessageResponse> AddChatMessageAsync(AddChatMessageRequest chatMessageRequest, int userId);
        Task<ChatMessageResponse> RegenerateChatMessageAsync(RegenerateMessageRequest request, int userId);
        Task<List<ChatSessionResponse>> GetChatSessionsByUserIdAsync(int userId);
        Task<List<ChatMessageResponse>> GetChatMessagesBySessionIdAsync(int sessionId);
    }

    public class ChatService : IChatService
    {
        private readonly KnowledgeContext _context;
        private readonly IChatClient _chatClient;
        private readonly IDocumentService _documentService;

        public ChatService(KnowledgeContext context, IChatClient chatClient, IDocumentService documentService)
        {
            _context = context;
            _chatClient = chatClient;
            _documentService = documentService;
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
            var chatSession = await GetChatSessionAsync(chatMessageRequest.ChatSessionId, userId);

            var userMessage = await SaveUserMessageAsync(chatSession, chatMessageRequest.Content);

            await AttachDocumentsToMessageAsync(
                chatSession.Id,
                userMessage.Id,
                chatMessageRequest.DocumentIds,
                userId);

            var documentSearchResults = await SearchDocumentContextAsync(
                chatMessageRequest.Content,
                chatMessageRequest.DocumentIds,
                userId);

            var chatMessages = await GetChatMessagesAsync(chatSession.Id);

            var messages = BuildChatMessages(chatMessages, documentSearchResults);

            var response = await _chatClient.GetResponseAsync(messages);

            var assistantMessage = await SaveAssistantMessageAsync(
                chatSession,
                response.Text);

            var newTitle = await GenerateTitleIfRequiredAsync(
                chatSession,
                chatMessages,
                messages);

            await _context.SaveChangesAsync();

            return new ChatMessageResponse
            {
                Id = assistantMessage.Id,
                ChatSessionId = assistantMessage.ChatSessionId,
                Role = assistantMessage.Role,
                Content = assistantMessage.Content,
                NewTitle = newTitle,
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

        public async Task<ChatMessageResponse> RegenerateChatMessageAsync(RegenerateMessageRequest request, int userId)
        {
            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(x => x.Id == request.ChatSessionId && x.UserId == userId);

            if (chatSession == null)
            {
                throw new InvalidOperationException("Chat session was not found.");
            }

            var chatMessages = await _context.ChatMessages
                .Where(x => x.ChatSessionId == chatSession.Id)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var target = chatMessages.FirstOrDefault(x => x.Id == request.ChatMessageId);

            if (target == null)
            {
                throw new InvalidOperationException("Chat message was not found.");
            }

            if (target.Role != "assistant")
            {
                throw new InvalidOperationException("Only assistant messages can be regenerated.");
            }

            // The client only swaps out a single message, so only allow the latest one.
            if (chatMessages[^1].Id != target.Id)
            {
                throw new InvalidOperationException("Only the latest assistant message can be regenerated.");
            }

            // Conversation history up to, but not including, the message being regenerated.
            var history = chatMessages
                .TakeWhile(x => x.Id != target.Id)
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

            var response = await _chatClient.GetResponseAsync(history);

            target.Content = response.Text;
            chatSession.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ChatMessageResponse
            {
                Id = target.Id,
                ChatSessionId = target.ChatSessionId,
                Role = target.Role,
                Content = target.Content,
                CreatedAt = target.CreatedAt
            };
        }

        private async Task<ChatSession> GetChatSessionAsync(int chatSessionId, int userId)
        {
            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(x => x.Id == chatSessionId && x.UserId == userId);

            if (chatSession == null)
            {
                throw new InvalidOperationException("Chat session was not found.");
            }

            return chatSession;
        }

        private async Task<Application.Entities.Chat.ChatMessage> SaveUserMessageAsync(ChatSession chatSession, string content)
        {
            var now = DateTime.UtcNow;

            var userMessage = new Application.Entities.Chat.ChatMessage
            {
                ChatSessionId = chatSession.Id,
                Role = "user",
                Content = content,
                CreatedAt = now
            };

            _context.ChatMessages.Add(userMessage);

            chatSession.UpdatedAt = now;

            await _context.SaveChangesAsync();

            return userMessage;
        }

        private async Task AttachDocumentsToMessageAsync(int chatSessionId, long messageId, List<int> documentIds, int userId)
        {
            if (!documentIds.Any())
            {
                return;
            }

            var documents = await _context.Documents
                .Where(x =>
                    documentIds.Contains(x.Id) &&
                    x.UserId == userId &&
                    x.ChatSessionId == chatSessionId)
                .ToListAsync();

            foreach (var document in documents)
            {
                document.ChatMessageId = messageId;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<List<DocumentSearchResult>> SearchDocumentContextAsync(string query, List<int> documentIds, int userId)
        {
            if (!documentIds.Any())
            {
                return new();
            }

            return await _documentService.SearchAsync(query, documentIds, userId);
        }

        private async Task<List<Application.Entities.Chat.ChatMessage>> GetChatMessagesAsync(int chatSessionId)
        {
            return await _context.ChatMessages
                .AsNoTracking()
                .Where(x => x.ChatSessionId == chatSessionId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        private static List<AIChatMessage> BuildChatMessages(List<Application.Entities.Chat.ChatMessage> chatMessages, List<DocumentSearchResult> documentSearchResults)
        {
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

            if (!documentSearchResults.Any())
            {
                return messages;
            }

            var documentContext = string.Join("\n\n",
                documentSearchResults.Select(x =>
                    $"[Document: {x.FileName}, Chunk: {x.ChunkIndex}]\n{x.Content}"));

            messages.Insert(0, new AIChatMessage
            {
                Role = ChatRole.System,
                Contents =
                [
                    new TextContent($"""
                        You are a helpful AI assistant.

                        The user has provided documents that may contain information relevant
                        to their question.

                        Use the document context below when answering the user's question.
                        Only use information from the context when answering questions about
                        the uploaded documents.

                        If the answer cannot be found in the provided document context, say
                        that you cannot find the answer in the uploaded documents. Do not
                        invent information.

                        DOCUMENT CONTEXT:

                        {documentContext}
                        """)
                ]
            });

            return messages;
        }

        private async Task<Application.Entities.Chat.ChatMessage> SaveAssistantMessageAsync(ChatSession chatSession, string content)
        {
            var assistantMessage = new Application.Entities.Chat.ChatMessage
            {
                ChatSessionId = chatSession.Id,
                Role = "assistant",
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(assistantMessage);

            chatSession.UpdatedAt = assistantMessage.CreatedAt;

            return assistantMessage;
        }

        private async Task<string?> GenerateTitleIfRequiredAsync(ChatSession chatSession, List<Application.Entities.Chat.ChatMessage> chatMessages, List<AIChatMessage> messages)
        {
            var userMessageCount = chatMessages.Count(x => x.Role == "user");

            if (chatSession.Title != "New Chat" || userMessageCount < 2)
            {
                return null;
            }

            var title = await GenerateChatTitleAsync(messages);

            chatSession.Title = title;

            return title;
        }
    }
    
}