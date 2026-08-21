using HomeNotes.Core.DTOs.Notes;
using HomeNotes.Core.DTOs.Sync;
using HomeNotes.Core.Exceptions;
using HomeNotes.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeNotes.Api.Server.Controllers
{
    [Authorize] 
    [Route("api")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly INotesService _notesService;

        public SyncController(INotesService notesService)
        {
            _notesService = notesService;
        }

        [HttpPost("sync")]
        [ProducesResponseType(typeof(SyncResponse), 200)]
        public async Task<IActionResult> Sync([FromBody] SyncRequest request)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _notesService.SyncAsync(request);
                return Ok(result);
            }
            catch (Exception ex) 
            {
                return BadRequest(ex.Message);
            }

        }

       
    }
}