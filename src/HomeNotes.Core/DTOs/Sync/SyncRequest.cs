using HomeNotes.Core.DTOs.Notes;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Sync
{
    public class SyncRequest
    {
        public DateTime? Since { get; set; }
        public List<NotesRequest> ChangedNotes { get; set; } = [];
        public List<Guid> DeletedNoteIds { get; set; } = []; 
    }
}
