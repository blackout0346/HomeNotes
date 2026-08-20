using HomeNotes.Core.DTOs.Notes;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Sync
{
    public class SyncPullResponse
    {
        public DateTimeKind ServerTime { get; set; }
        public List<NotesResponse> Notes { get; set; } = [];
    }
}
