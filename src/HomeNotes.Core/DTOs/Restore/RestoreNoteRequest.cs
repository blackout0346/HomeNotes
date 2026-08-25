using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Restore
{
    public class RestoreNoteRequest
    {
        public List<Guid> NoteIds { get; set; } = [];
    }
}
