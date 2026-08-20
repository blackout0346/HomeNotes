using HomeNotes.Core.DTOs.Notes;
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
    }
}