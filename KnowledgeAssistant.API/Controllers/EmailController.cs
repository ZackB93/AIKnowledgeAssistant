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
        public async Task<ActionResult<ApiResult>> GetEmail(int id)
        {
            var email = await _emailService.GetEmailByIdAsync(id);

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
        public async Task<ActionResult<ApiResult>> GetEmailsByUserId(int userId)
        {
            var emails = await _emailService.GetEmailsByUserIdAsync(userId);

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
        public async Task<ActionResult<ApiResult>> GetEmails(int pageNumber, int? pageSize = 10)
        {
            var emails = await _emailService.GetEmailsAsync(pageNumber, (int)pageSize!);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = emails
            });
        }

        [HttpGet("Search")]
        [Authorize]
        public async Task<ActionResult<ApiResult>> Search(string searchTerm, int pageNumber, int? pageSize = 10)
        {
            var searchedEmails = await _emailService.SearchEmailsAsync(searchTerm, pageNumber, (int)pageSize!);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = searchedEmails
            });
        }
    }
}
