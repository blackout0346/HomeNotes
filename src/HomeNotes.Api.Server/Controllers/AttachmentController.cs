using HomeNotes.Core.DTOs.Attachments;
using HomeNotes.Core.Exceptions;
using HomeNotes.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeNotes.Api.Server.Controllers
{
    [Authorize]
    [Route("api")]
    [ApiController]
    public class AttachmentController : ControllerBase
    {
        private readonly IAttachmentsService _attachmentService;

        public AttachmentController(IAttachmentsService attachmentsService)
        {
            _attachmentService = attachmentsService;
        }

        [HttpPost("attachments")]
        [ProducesResponseType(typeof(AttachmentsResponse), 200)]
        public async Task<IActionResult> CreateAttachment([FromForm] AttachmentsRequest request)
        {
            try
            {
                var result = await _attachmentService.CreateAttachAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
  
                return NotFound(new { message = "Note not found." });
            }
        }

        [HttpGet("attachments/{id:guid}")]
        [ProducesResponseType(typeof(AttachmentsResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAttachment(Guid id)
        {
            try
            {
                var result = await _attachmentService.GetAttachAsync(id);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
 
                return NotFound();
            }
        }

        [HttpPut("attachments/{id:guid}")]
        [ProducesResponseType(typeof(AttachmentsResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateAttachment(Guid id, [FromForm] AttachmentsRequest request)
        {
            try
            {
                var result = await _attachmentService.UpdateAttachAsync(id, request);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }

        [HttpDelete("attachments/{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAttachment(Guid id)
        {
            try
            {
                await _attachmentService.DeleteAsync(id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }

        [HttpGet("attachments/{id:guid}/content")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAttachmentContent(Guid id)
        {
            try
            {
                var (stream, mimeType, fileName) = await _attachmentService.GetAttachContentAsync(id);
  
                return File(stream, mimeType, fileName);
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPut("attachments/{id:guid}/content")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [RequestSizeLimit(500_000_000)]
        public async Task<IActionResult> UpdateAttachmentContent(Guid id)
        {
            try
            {
        
                await _attachmentService.UpdateAttachContentAsync(id, Request.Body);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
    }
}