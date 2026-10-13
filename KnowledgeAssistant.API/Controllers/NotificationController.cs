using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.DTOs.Notifications;
using KnowledgeAssistant.Application.Services.Communication;
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

        [HttpPost("Send")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> Send(CreateNotificationRequest request, CancellationToken ct)
        {
            if (!request.UserIds.Any())
            {
                return BadRequest(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "At least one user recipient ID is required."
                });
            }

            var notification = await _notificationService.SendNotificationAsync(request, ct);
            var result = new ApiResult
            {
                IsSuccessful = true,
                Data = notification,
                Message = "Notification sent successfully."
            };

            return Ok(result);
        }


        [HttpGet("GetById")]
        [Authorize(Roles = "Admin")]
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

        [HttpGet("GetByUserId")]
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> GetNotifications(int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var notifications = await _notificationService.GetNotificationsAsync(pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = notifications
            });
        }

        [HttpGet("GetRecipients")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> GetRecipients(int notificationId, int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var recipients = await _notificationService.GetRecipientsAsync(notificationId, pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = recipients
            });
        }

        [HttpGet("Search")]
        [Authorize(Roles = "Admin")]
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