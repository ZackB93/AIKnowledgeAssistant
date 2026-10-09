using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Roles;
using KnowledgeAssistant.Application.Services.Communication;
using KnowledgeAssistant.Application.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeAssistant.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("GetById/{Id}")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetNotificationById(int id, CancellationToken ct)
        {
            var notification = await _notificationService.GetNotificationByIdAsync(id, ct);

            if (notification is null)
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "Notification does not exist."
                });
            }

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = notification
            });
        }

        [HttpGet("GetByUserId/{Id}")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetNotificationsByUserId(int userId, int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = notifications
            });
        }

        [HttpGet("GetAll")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetNotifications(int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var notifications = await _notificationService.GetNotificationsAsync(pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = notifications
            });
        }

        [HttpGet("Search")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> Search(string searchTerm, int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var searchedNotifications = await _notificationService.SearchNotificationsAsync(searchTerm, pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = searchedNotifications
            });
        }

    }
}