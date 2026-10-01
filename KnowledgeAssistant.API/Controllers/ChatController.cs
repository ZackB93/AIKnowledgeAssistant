using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Chat;
using KnowledgeAssistant.Application.DTOs.Users;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KnowledgeAssistant.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService ChatService)
        {
            _chatService = ChatService;
        }

        [HttpGet("GetChatSessionsByUserId")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetChatSessionsByUserIdAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var chatSessions = await _chatService.GetChatSessionsByUserIdAsync(userId);

            if (!chatSessions.Any())
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "No chat sessions found for user."
                });
            }

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = chatSessions
            });
        }

        [HttpGet("GetChatMessagesBySessionId/{sessionId}")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetChatMessagesBySessionIdAsync(int sessionId)
        {
            var chatSessionMessages = await _chatService.GetChatMessagesBySessionIdAsync(sessionId);

            if (!chatSessionMessages.Any())
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "No chat messages for session found for user."
                });
            }

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = chatSessionMessages
            });
        }

        [HttpPost("AddSession")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> AddChatSession(AddChatSessionRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var addedChatSession = await _chatService.AddChatSessionAsync(request, userId);
            var result = new ApiResult
            {
                IsSuccessful = true,
                Data = addedChatSession,
                Message = "Chat session added successfully."
            };

            return Ok(result);
        }

        [HttpPost("AddMessage")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> AddChatMessage(AddChatMessageRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var chatResponse = await _chatService.AddChatMessageAsync(request, userId);

            var result = new ApiResult
            {
                IsSuccessful = true,
                Data = chatResponse,
                Message = "Chat message added successfully."
            };

            return Ok(result);
        }
    }
}
