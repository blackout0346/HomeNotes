using HomeNotes.Core.DTOs.Notes;
using HomeNotes.Core.DTOs.Restore;
using HomeNotes.Core.Exceptions;
using HomeNotes.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeNotes.Api.Server.Controllers
{
    [Authorize] 
    [Route("api")]
    [ApiController]
    public class NoteController : ControllerBase
    {
        private readonly INotesService _notesService;

        public NoteController(INotesService notesService)
        {
            _notesService = notesService;
        }

        [HttpPost("notes")]
        [ProducesResponseType(typeof(NotesResponse), 200)]
        public async Task<IActionResult> CreateNote([FromBody] NotesRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {

                var result = await _notesService.CreateNoteAsync(request);
                return Ok(result);
            }
            catch (ConflictResponse ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("notes")]
        [ProducesResponseType(typeof(IEnumerable<NotesResponse>), 200)]
        public async Task<IActionResult> GetNotes()
        {
    
            var result = await _notesService.GetListAsync();
            return Ok(result);
        }

        [HttpGet("notes/{id:guid}")]
        [ProducesResponseType(typeof(NotesResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetNoteById(Guid id)
        {
            try
            {
                var result = await _notesService.GetByIdAsync(id);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
           
                return NotFound();
            }
        }

        [HttpPut("notes/{id:guid}")]
        [ProducesResponseType(typeof(NotesResponse), 200)]
        public async Task<IActionResult> UpdateNote(Guid id, [FromBody] NotesRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _notesService.UpdateNoteAsync(id, request);
                return Ok(result);
            }
            catch (ConflictResponse ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }

        [HttpDelete("notes/{id:guid}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteNote(Guid id)
        {
            try
            {
                await _notesService.DeleteNoteAsync(id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
        [HttpGet("notes/{id:guid}/content")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetNoteContent(Guid id)
        {
            try
            {
                var stream = await _notesService.GetNoteContentAsync(id);
                return File(stream, "text/markdown");
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
        [HttpPut("notes/{id:guid}/content")]
        [ProducesResponseType(404)]
        [RequestSizeLimit(500_000_000)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> UpdateContent(Guid id)
        {
            try
            {
                await _notesService.UpdateNoteContentAsync(id, Request.Body);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
        [HttpPut("notes/restore")]
        [ProducesResponseType(typeof(IEnumerable<NotesResponse>), 200)]
        public async Task<IActionResult> RestoreNotes([FromBody] RestoreNoteRequest request)
        {
            try
            {
              var result = await _notesService.RestoreNotesAsync(request.NoteIds);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
    }
}