using HomeNotes.Core.DTOs.Attachments;
using HomeNotes.Core.Exceptions;
using HomeNotes.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;

namespace HomeNotes.Api.Server.Controllers
{

    [Route("api")]
    [ApiController]
    public class AttachmentController : ControllerBase
    {
        public readonly IAttachmentsService _attachmentService;
        public AttachmentController(IAttachmentsService attachmentsService) 
        {
            _attachmentService = attachmentsService;
        }
        [HttpGet("attachment/{attachmentId}")]
        [ProducesResponseType(typeof(AttachmentsResponse), 200)]
        public async Task<IActionResult> GetAttachment([FromRoute] Guid attachmentId)
        {
            try
            {
                var result = await _attachmentService.GetAttachAsync(attachmentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}