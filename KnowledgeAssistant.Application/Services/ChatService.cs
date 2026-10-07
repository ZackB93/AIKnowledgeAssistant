using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Application.DTOs.Documents;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.Chat;
using Microsoft.Extensions.AI;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatMessage = KnowledgeAssistant.Domain.Entities.Chat.ChatMessage;

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
        private readonly IChatRepository _chatRepository;
        private readonly IChatClient _chatClient;
        private readonly IDocumentService _documentService;

        public ChatService(
            IChatRepository chatRepository,
            IChatClient chatClient,
            IDocumentService documentService)
        {
            _chatRepository = chatRepository;
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

            await _chatRepository.AddChatSessionAsync(chatSession);
            await _chatRepository.SaveChangesAsync();

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

            var chatMessages = await _chatRepository.GetChatMessagesBySessionIdAsync(chatSession.Id);

            var messages = BuildChatMessages(chatMessages, documentSearchResults);

            var response = await _chatClient.GetResponseAsync(messages);

            var assistantMessage = await SaveAssistantMessageAsync(
                chatSession,
                response.Text);

            var newTitle = await GenerateTitleIfRequiredAsync(
                chatSession,
                chatMessages,
                messages);

            await _chatRepository.SaveChangesAsync();

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
            return await _chatRepository.GetChatSessionsByUserIdAsync(userId);
        }

        public async Task<List<ChatMessageResponse>> GetChatMessagesBySessionIdAsync(int sessionId)
        {
            var messages = await _chatRepository.GetChatMessagesBySessionIdAsync(sessionId);

            return messages.Select(x => new ChatMessageResponse
            {
                Id = x.Id,
                ChatSessionId = x.ChatSessionId,
                Role = x.Role,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            }).ToList();
        }

        public async Task<ChatMessageResponse> RegenerateChatMessageAsync(RegenerateMessageRequest request, int userId)
        {
            var chatSession = await _chatRepository.GetChatSessionByIdAndUserIdAsync(request.ChatSessionId, userId);

            if (chatSession == null)
            {
                throw new InvalidOperationException("Chat session was not found.");
            }

            var chatMessages = await _chatRepository.GetChatMessagesBySessionIdAsync(chatSession.Id);

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

            await _chatRepository.SaveChangesAsync();

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
            var chatSession = await _chatRepository.GetChatSessionByIdAndUserIdAsync(chatSessionId, userId);

            if (chatSession == null)
            {
                throw new InvalidOperationException("Chat session was not found.");
            }

            return chatSession;
        }

        private async Task<ChatMessage> SaveUserMessageAsync(ChatSession chatSession, string content)
        {
            var now = DateTime.UtcNow;

            var userMessage = new ChatMessage
            {
                ChatSessionId = chatSession.Id,
                Role = "user",
                Content = content,
                CreatedAt = now
            };

            await _chatRepository.AddChatMessageAsync(userMessage);

            chatSession.UpdatedAt = now;

            return userMessage;
        }

        private async Task AttachDocumentsToMessageAsync(int chatSessionId, long messageId, List<int> documentIds, int userId)
        {
            if (!documentIds.Any())
            {
                return;
            }

            await _chatRepository.AttachDocumentsToMessageAsync(chatSessionId, messageId, documentIds, userId);
        }

        private async Task<List<DocumentSearchResult>> SearchDocumentContextAsync(string query, List<int> documentIds, int userId)
        {
            if (!documentIds.Any())
            {
                return new();
            }

            return await _documentService.SearchAsync(query, documentIds, userId);
        }

        private static List<AIChatMessage> BuildChatMessages(List<ChatMessage> chatMessages, List<DocumentSearchResult> documentSearchResults)
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

        private async Task<ChatMessage> SaveAssistantMessageAsync(ChatSession chatSession, string content)
        {
            var assistantMessage = new ChatMessage
            {
                ChatSessionId = chatSession.Id,
                Role = "assistant",
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            await _chatRepository.AddChatMessageAsync(assistantMessage);

            chatSession.UpdatedAt = assistantMessage.CreatedAt;

            return assistantMessage;
        }

        private async Task<string?> GenerateTitleIfRequiredAsync(ChatSession chatSession, List<ChatMessage> chatMessages, List<AIChatMessage> messages)
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