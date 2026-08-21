using HomeNotes.Core.DTOs.Notes;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Sync
{
    public class SyncConflict
    {
        public Guid NoteId { get; set; }
        public NotesResponse ServerVersion { get; set; } = null!;
        public NotesRequest ClientVersion { get; set; } = null!;
    }
}
