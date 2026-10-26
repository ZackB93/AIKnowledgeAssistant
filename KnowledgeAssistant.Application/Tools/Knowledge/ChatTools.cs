using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Application.Services.Identity;

namespace KnowledgeAssistant.Application.Tools.Knowledge
{
    public interface IChatTools
    {
        Task<List<ChatSessionResponse>> GetRecentChats(int count, CancellationToken ct);

        Task<List<ChatHistorySearchResult>> SearchChatHistory(string query, CancellationToken ct);

        Task<string> GetChatSummary(int chatSessionId, CancellationToken ct);
    }

    public class ChatTools : IChatTools
    {
        private readonly IChatRepository _chatRepository;
        private readonly ICurrentUserService _currentUserService;

        public ChatTools(IChatRepository chatRepository, ICurrentUserService currentUserService)
        {
            _chatRepository = chatRepository;
            _currentUserService = currentUserService;
        }

        public async Task<List<ChatSessionResponse>> GetRecentChats(int count, CancellationToken ct)
        {
            return await _chatRepository.GetRecentChatSessionsByUserIdAsync(_currentUserService.UserId, Math.Clamp(count, 1, 20), ct);
        }

        public async Task<List<ChatHistorySearchResult>> SearchChatHistory(string query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new();
            }

            return await _chatRepository.SearchChatHistoryAsync(_currentUserService.UserId, query.Trim(), 20, ct);
        }

        public async Task<string> GetChatSummary(int chatSessionId, CancellationToken ct)
        {
            if (chatSessionId <= 0)
            {
                return "A valid chat session ID is required.";
            }

            var messages = await _chatRepository.GetChatMessagesBySessionIdAndUserIdAsync(chatSessionId, _currentUserService.UserId, 50, ct);

            if (messages.Count == 0)
            {
                return "No messages were found for that chat, or the chat is unavailable.";
            }

            return string.Join(Environment.NewLine, messages.Select(x => $"{x.Role}: {x.Content}"));
        }
    }
}

