using KnowledgeAssistant.Application.DTOs.API;
using KnowledgeAssistant.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KnowledgeAssistant.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost("Upload")]
        [RequestSizeLimit(11 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken cancellationToken = default, [FromForm] int? chatSessionId = null)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var response = await _documentService.UploadAsync(file, userId, cancellationToken, chatSessionId);

            return Ok(new ApiResult
            {
                IsSuccessful = true,
                Data = response
            });
        }

        [HttpGet("GetAll")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResult>> GetDocuments(int pageNumber, int? pageSize = 10)
        {
            var documents = await _documentService.GetDocumentsAsync(pageNumber, (int)pageSize!);

            return Ok(new ApiResult()
            {
                IsSuccessful = true,
                Data = documents
            });
        }
    }
}
