using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeAssistant.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }
  
        [HttpGet("GetById/{Id}")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetEmail(int id, CancellationToken ct)
        {
            var email = await _emailService.GetEmailByIdAsync(id, ct);

            if (email is null)
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "Email does not exist."
                });
            }

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = email
            });
        }

        [HttpGet("GetByUserId/{Id}")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetEmailsByUserId(int userId, CancellationToken ct)
        {
            var emails = await _emailService.GetEmailsByUserIdAsync(userId, ct);

            if (!emails.Any())
            {
                return NotFound(new ApiResult()
                {
                    IsSuccessful = false,
                    Message = "No emails found for user"
                });
            }

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = emails
            });
        }

        [HttpGet("GetAll")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> GetEmails(int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var emails = await _emailService.GetEmailsAsync(pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = emails
            });
        }

        [HttpGet("Search")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> Search(string searchTerm, int pageNumber, CancellationToken ct, int? pageSize = 10)
        {
            var searchedEmails = await _emailService.SearchEmailsAsync(searchTerm, pageNumber, (int)pageSize!, ct);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = searchedEmails
            });
        }
    }
}
